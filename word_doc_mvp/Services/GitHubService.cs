using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;
using word_doc_mvp.Models;

namespace word_doc_mvp.Services
{
    public class GitHubService
    {
        private readonly GitHubClient _client;
        private readonly string _owner;

        public GitHubService(GitHubSettings settings)
        {
            _owner = settings.Username;
            _client = new GitHubClient(new ProductHeaderValue("word-doc-mvp"))
            {
                Credentials = new Credentials(settings.Token)
            };
        }

        /// <summary>
        /// Tests connectivity by fetching the authenticated user.
        /// Returns the login name on success.
        /// </summary>
        public async Task<string> TestConnectionAsync()
        {
            var user = await _client.User.Current();
            return user.Login;
        }

        /// <summary>
        /// Ensures the target repository exists. Creates it if missing.
        /// </summary>
        public async Task<Repository> EnsureRepositoryAsync(string repoName)
        {
            try
            {
                return await _client.Repository.Get(_owner, repoName);
            }
            catch (NotFoundException)
            {
                var newRepo = new NewRepository(repoName)
                {
                    AutoInit = true,
                    Description = "DOCX version control - normalized XML tracking",
                    Private = false
                };
                return await _client.Repository.Create(newRepo);
            }
        }

        /// <summary>
        /// Full pipeline: creates a branch, commits the normalized XML, and opens a PR.
        /// Returns the HTML URL of the created pull request.
        /// </summary>
        public async Task<string> CreatePullRequestWithNormalizedXmlAsync(
            string repoName,
            string normalizedXml,
            string branchName,
            string commitMessage,
            Action<string> log = null)
        {
            log?.Invoke("Ensuring repository exists...");
            var repo = await EnsureRepositoryAsync(repoName);
            long repoId = repo.Id;

            // Determine the default branch (usually "main" or "master")
            string defaultBranch = repo.DefaultBranch ?? "main";

            log?.Invoke($"Getting HEAD of '{defaultBranch}' branch...");
            Reference mainRef;
            try
            {
                mainRef = await _client.Git.Reference.Get(
                    _owner, repoName, $"heads/{defaultBranch}");
            }
            catch (NotFoundException)
            {
                // Repository is empty — push initial commit directly to default branch
                log?.Invoke("Repository is empty. Creating initial commit on default branch...");
                await CreateInitialCommitAsync(repoName, defaultBranch, normalizedXml, commitMessage, log);
                log?.Invoke("Initial commit pushed. No PR needed for the first version.");
                return repo.HtmlUrl;
            }

            string baseSha = mainRef.Object.Sha;

            log?.Invoke($"Creating branch '{branchName}'...");
            await _client.Git.Reference.Create(
                _owner, repoName,
                new NewReference($"refs/heads/{branchName}", baseSha));

            log?.Invoke("Uploading normalized XML blob...");
            var blob = await _client.Git.Blob.Create(
                _owner, repoName,
                new NewBlob
                {
                    Content = normalizedXml,
                    Encoding = EncodingType.Utf8
                });

            log?.Invoke("Building tree...");
            var baseCommit = await _client.Git.Commit.Get(_owner, repoName, baseSha);
            var newTree = await _client.Git.Tree.Create(
                _owner, repoName,
                new NewTree
                {
                    BaseTree = baseCommit.Tree.Sha,
                    Tree =
                    {
                        new NewTreeItem
                        {
                            Path = "document.xml",
                            Mode = "100644",
                            Type = TreeType.Blob,
                            Sha = blob.Sha
                        }
                    }
                });

            log?.Invoke("Creating commit...");
            var newCommit = await _client.Git.Commit.Create(
                _owner, repoName,
                new NewCommit(commitMessage, newTree.Sha, baseSha));

            log?.Invoke("Updating branch reference...");
            await _client.Git.Reference.Update(
                _owner, repoName,
                $"heads/{branchName}",
                new ReferenceUpdate(newCommit.Sha));

            log?.Invoke("Creating pull request...");
            var pr = await _client.PullRequest.Create(
                _owner, repoName,
                new NewPullRequest(commitMessage, branchName, defaultBranch)
                {
                    Body = $"Automated document update.\n\n" +
                           $"Branch: `{branchName}`\n" +
                           $"Committed at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC"
                });

            log?.Invoke($"Pull request created: {pr.HtmlUrl}");
            return pr.HtmlUrl;
        }

        /// <summary>
        /// Creates the very first commit in an empty repository by building
        /// a tree and commit from scratch, then pointing the default branch at it.
        /// </summary>
        private async Task CreateInitialCommitAsync(
            string repoName,
            string defaultBranch,
            string normalizedXml,
            string commitMessage,
            Action<string> log)
        {
            log?.Invoke("Uploading blob for initial commit...");
            var blob = await _client.Git.Blob.Create(
                _owner, repoName,
                new NewBlob
                {
                    Content = normalizedXml,
                    Encoding = EncodingType.Utf8
                });

            log?.Invoke("Creating tree...");
            var tree = await _client.Git.Tree.Create(
                _owner, repoName,
                new NewTree
                {
                    Tree =
                    {
                        new NewTreeItem
                        {
                            Path = "document.xml",
                            Mode = "100644",
                            Type = TreeType.Blob,
                            Sha = blob.Sha
                        }
                    }
                });

            log?.Invoke("Creating initial commit...");
            var commit = await _client.Git.Commit.Create(
                _owner, repoName,
                new NewCommit(commitMessage, tree.Sha));

            log?.Invoke("Setting default branch reference...");
            try
            {
                await _client.Git.Reference.Update(
                    _owner, repoName,
                    $"heads/{defaultBranch}",
                    new ReferenceUpdate(commit.Sha));
            }
            catch (NotFoundException)
            {
                await _client.Git.Reference.Create(
                    _owner, repoName,
                    new NewReference($"refs/heads/{defaultBranch}", commit.Sha));
            }
        }

        /// <summary>
        /// Returns the names of all branches in the repository.
        /// </summary>
        public async Task<List<string>> ListBranchesAsync(string repoName)
        {
            var branches = await _client.Repository.Branch.GetAll(_owner, repoName);
            return branches.Select(b => b.Name).ToList();
        }

        /// <summary>
        /// Fetches the text content of a file from a specific branch.
        /// Uses the Contents API first; falls back to the Git Blob API
        /// for files that exceed the 1 MB Contents API limit.
        /// </summary>
        public async Task<string> GetFileContentAsync(
            string repoName, string branchName, string filePath,
            Action<string> log = null)
        {
            log?.Invoke($"[DEBUG] GetFileContentAsync: repo={repoName}, branch={branchName}, file={filePath}");

            // --- Attempt 1: Contents API ---
            try
            {
                log?.Invoke("Fetching via Contents API...");
                var contents = await _client.Repository.Content.GetAllContentsByRef(
                    _owner, repoName, filePath, branchName);

                log?.Invoke($"[DEBUG] Contents API returned {contents.Count} item(s).");

                if (contents.Count > 0)
                {
                    var item = contents[0];
                    log?.Invoke($"[DEBUG] Item: Name={item.Name}, Size={item.Size}, " +
                                $"Type={item.Type}, Sha={item.Sha}");
                    log?.Invoke($"[DEBUG] Content is null: {item.Content == null}, " +
                                $"Content length: {item.Content?.Length ?? -1}");
                    log?.Invoke($"[DEBUG] EncodedContent is null: {item.EncodedContent == null}, " +
                                $"EncodedContent length: {item.EncodedContent?.Length ?? -1}");

                    // Primary: use decoded Content if available
                    if (!string.IsNullOrEmpty(item.Content))
                    {
                        log?.Invoke($"Contents API returned {item.Content.Length:N0} decoded chars.");
                        return item.Content;
                    }

                    // Fallback: decode EncodedContent if Content was null
                    if (!string.IsNullOrEmpty(item.EncodedContent))
                    {
                        log?.Invoke("Content was empty but EncodedContent available, decoding base64...");
                        string cleaned = item.EncodedContent.Replace("\n", "").Replace("\r", "");
                        byte[] bytes = Convert.FromBase64String(cleaned);
                        string decoded = Encoding.UTF8.GetString(bytes);
                        log?.Invoke($"Decoded EncodedContent: {decoded.Length:N0} characters.");
                        return decoded;
                    }
                }

                log?.Invoke("Contents API returned no usable content, falling back to Blob API...");
            }
            catch (Octokit.NotFoundException)
            {
                throw new InvalidOperationException(
                    $"File '{filePath}' not found on branch '{branchName}'.");
            }
            catch (Exception ex)
            {
                log?.Invoke($"[DEBUG] Contents API exception: {ex.GetType().Name}: {ex.Message}");
                log?.Invoke("Falling back to Blob API...");
            }

            // --- Attempt 2: Git Blob API ---
            return await GetFileViaBlobApiAsync(repoName, branchName, filePath, log);
        }

        private async Task<string> GetFileViaBlobApiAsync(
            string repoName, string branchName, string filePath,
            Action<string> log)
        {
            log?.Invoke($"[Blob API] Resolving branch '{branchName}' reference...");
            var branchRef = await _client.Git.Reference.Get(
                _owner, repoName, $"heads/{branchName}");

            var commitSha = branchRef.Object.Sha;
            log?.Invoke($"[DEBUG] Commit SHA: {commitSha}");

            var commit = await _client.Git.Commit.Get(_owner, repoName, commitSha);
            log?.Invoke($"[DEBUG] Tree SHA: {commit.Tree.Sha}");

            log?.Invoke("[Blob API] Walking tree to locate file blob...");
            var tree = await _client.Git.Tree.GetRecursive(_owner, repoName, commit.Tree.Sha);
            log?.Invoke($"[DEBUG] Tree has {tree.Tree.Count} items, truncated={tree.Truncated}");

            var treeItem = tree.Tree.FirstOrDefault(
                t => t.Path.Equals(filePath, StringComparison.OrdinalIgnoreCase));

            if (treeItem == null)
            {
                log?.Invoke($"[DEBUG] File not found in tree. Available files:");
                foreach (var t in tree.Tree)
                    log?.Invoke($"[DEBUG]   {t.Path} ({t.Type}, {t.Size} bytes)");

                throw new InvalidOperationException(
                    $"File '{filePath}' not found in tree for branch '{branchName}'.");
            }

            log?.Invoke($"[Blob API] Found: {treeItem.Path}, size={treeItem.Size:N0} bytes, sha={treeItem.Sha}");
            var blob = await _client.Git.Blob.Get(_owner, repoName, treeItem.Sha);

            log?.Invoke($"[DEBUG] Blob encoding: {blob.Encoding}");
            log?.Invoke($"[DEBUG] Blob content is null: {blob.Content == null}");
            log?.Invoke($"[DEBUG] Blob content length: {blob.Content?.Length ?? -1}");

            if (blob.Content == null)
                throw new InvalidOperationException(
                    $"GitHub returned null blob content for '{filePath}'. File may be too large for Blob API.");

            if (blob.Encoding == EncodingType.Base64)
            {
                log?.Invoke("[Blob API] Decoding base64 content...");
                string cleaned = blob.Content.Replace("\n", "").Replace("\r", "");
                byte[] bytes = Convert.FromBase64String(cleaned);
                string result = Encoding.UTF8.GetString(bytes);
                log?.Invoke($"[Blob API] Decoded: {result.Length:N0} characters.");
                return result;
            }

            log?.Invoke($"[Blob API] Returning raw UTF-8: {blob.Content.Length:N0} characters.");
            return blob.Content;
        }
    }
}
