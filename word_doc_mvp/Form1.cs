using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using word_doc_mvp.Models;
using word_doc_mvp.Services;

namespace word_doc_mvp
{
    public partial class Form1 : Form
    {
        private string _selectedFilePath;

        public Form1()
        {
            InitializeComponent();
            LoadSettingsIntoUI();
            GenerateDefaultBranchName();
        }

        private void LoadSettingsIntoUI()
        {
            var settings = GitHubSettings.LoadFromConfig();
            txtUsername.Text = settings.Username;
            txtToken.Text = settings.Token;
            txtRepo.Text = settings.RepositoryName;
        }

        private void GenerateDefaultBranchName()
        {
            txtBranchName.Text = $"doc-update-{DateTime.Now:yyyy-MM-dd-HHmm}";
        }

        private GitHubSettings GetSettingsFromUI()
        {
            return new GitHubSettings
            {
                Username = txtUsername.Text.Trim(),
                Token = txtToken.Text.Trim(),
                RepositoryName = txtRepo.Text.Trim()
            };
        }

        #region Logging

        private void Log(string message)
        {
            if (rtbLog.InvokeRequired)
            {
                rtbLog.Invoke(new Action<string>(Log), message);
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            rtbLog.AppendText($"[{timestamp}] {message}\n");
            rtbLog.ScrollToCaret();
        }

        private void LogError(string message)
        {
            if (rtbLog.InvokeRequired)
            {
                rtbLog.Invoke(new Action<string>(LogError), message);
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionColor = System.Drawing.Color.OrangeRed;
            rtbLog.AppendText($"[{timestamp}] ERROR: {message}\n");
            rtbLog.SelectionColor = rtbLog.ForeColor;
            rtbLog.ScrollToCaret();
        }

        private void LogSuccess(string message)
        {
            if (rtbLog.InvokeRequired)
            {
                rtbLog.Invoke(new Action<string>(LogSuccess), message);
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionColor = System.Drawing.Color.LimeGreen;
            rtbLog.AppendText($"[{timestamp}] {message}\n");
            rtbLog.SelectionColor = rtbLog.ForeColor;
            rtbLog.ScrollToCaret();
        }

        #endregion

        #region UI Event Handlers

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                _selectedFilePath = openFileDialog.FileName;
                lblSelectedFile.Text = Path.GetFileName(_selectedFilePath);
                lblSelectedFile.ForeColor = System.Drawing.Color.Black;
                Log($"Selected: {_selectedFilePath}");
            }
        }

        private async void btnTestConnection_Click(object sender, EventArgs e)
        {
            var settings = GetSettingsFromUI();
            if (string.IsNullOrWhiteSpace(settings.Token))
            {
                LogError("GitHub Token is required.");
                return;
            }

            btnTestConnection.Enabled = false;
            try
            {
                Log("Testing GitHub connection...");
                var service = new GitHubService(settings);
                string login = await service.TestConnectionAsync();
                LogSuccess($"Connected as: {login}");
            }
            catch (Exception ex)
            {
                LogError($"Connection failed: {ex.Message}");
            }
            finally
            {
                btnTestConnection.Enabled = true;
            }
        }

        private async void btnProcess_Click(object sender, EventArgs e)
        {
            // --- Input validation ---
            if (string.IsNullOrWhiteSpace(_selectedFilePath) || !File.Exists(_selectedFilePath))
            {
                LogError("Please select a valid DOCX file first.");
                return;
            }

            if (!_selectedFilePath.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                LogError("Selected file is not a .docx file.");
                return;
            }

            var settings = GetSettingsFromUI();
            if (!settings.IsValid())
            {
                LogError("GitHub settings are incomplete. Fill in Username, Token, and Repository.");
                return;
            }

            string branchName = txtBranchName.Text.Trim();
            if (string.IsNullOrWhiteSpace(branchName))
            {
                LogError("Branch name cannot be empty.");
                return;
            }

            string commitMessage = txtCommitMessage.Text.Trim();
            if (string.IsNullOrWhiteSpace(commitMessage))
                commitMessage = "Update document";

            // --- Disable UI during processing ---
            SetProcessingState(true);
            linkPR.Text = "";

            try
            {
                // Step 1: Normalize
                Log("=== Starting Normalization Pipeline ===");
                string normalizedXml = null;

                await System.Threading.Tasks.Task.Run(() =>
                {
                    normalizedXml = DocxNormalizerService.NormalizeDocx(
                        _selectedFilePath, msg => Log(msg));
                });

                LogSuccess($"Normalized XML size: {normalizedXml.Length:N0} characters");

                // Step 2: Push to GitHub as PR
                Log("=== Starting GitHub Push ===");
                var gitHubService = new GitHubService(settings);

                string prUrl = await gitHubService.CreatePullRequestWithNormalizedXmlAsync(
                    settings.RepositoryName,
                    normalizedXml,
                    branchName,
                    commitMessage,
                    msg => Log(msg));

                LogSuccess($"Done! PR URL: {prUrl}");
                linkPR.Text = prUrl;

                // Auto-generate next branch name for convenience
                GenerateDefaultBranchName();
            }
            catch (Exception ex)
            {
                LogError($"{ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                    LogError($"  Inner: {ex.InnerException.Message}");
            }
            finally
            {
                SetProcessingState(false);
            }
        }

        private void linkPR_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(linkPR.Text))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = linkPR.Text,
                    UseShellExecute = true
                });
            }
        }

        private async void btnRefreshBranches_Click(object sender, EventArgs e)
        {
            var settings = GetSettingsFromUI();
            if (!settings.IsValid())
            {
                LogError("GitHub settings are incomplete. Fill in Username, Token, and Repository.");
                return;
            }

            btnRefreshBranches.Enabled = false;
            try
            {
                Log("Fetching branches...");
                var service = new GitHubService(settings);
                var branches = await service.ListBranchesAsync(settings.RepositoryName);

                cmbBranches.Items.Clear();
                foreach (var branch in branches)
                    cmbBranches.Items.Add(branch);

                if (cmbBranches.Items.Count > 0)
                    cmbBranches.SelectedIndex = 0;

                LogSuccess($"Found {branches.Count} branch(es).");
            }
            catch (Exception ex)
            {
                LogError($"Failed to fetch branches: {ex.Message}");
            }
            finally
            {
                btnRefreshBranches.Enabled = true;
            }
        }

        private async void btnDownloadDocx_Click(object sender, EventArgs e)
        {
            if (cmbBranches.SelectedItem == null)
            {
                LogError("Select a branch first. Click Refresh to load branches.");
                return;
            }

            var settings = GetSettingsFromUI();
            if (!settings.IsValid())
            {
                LogError("GitHub settings are incomplete. Fill in Username, Token, and Repository.");
                return;
            }

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            string branch = cmbBranches.SelectedItem.ToString();
            string savePath = saveFileDialog.FileName;

            SetProcessingState(true);
            try
            {
                Log($"=== Downloading from branch '{branch}' ===");
                var service = new GitHubService(settings);

                string xmlContent = await service.GetFileContentAsync(
                    settings.RepositoryName, branch, "document.xml",
                    msg => Log(msg));

                LogSuccess($"Downloaded XML: {xmlContent.Length:N0} characters");

                Log("Reconstructing DOCX...");
                await System.Threading.Tasks.Task.Run(() =>
                {
                    DocxReconstructorService.ReconstructDocx(
                        xmlContent, savePath, msg => Log(msg));
                });

                LogSuccess($"DOCX saved to: {savePath}");
            }
            catch (Exception ex)
            {
                LogError($"{ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                    LogError($"  Inner: {ex.InnerException.Message}");
            }
            finally
            {
                SetProcessingState(false);
            }
        }

        #endregion

        private void SetProcessingState(bool processing)
        {
            btnProcess.Enabled = !processing;
            btnSelectFile.Enabled = !processing;
            btnTestConnection.Enabled = !processing;
            grpGitHub.Enabled = !processing;
            txtBranchName.Enabled = !processing;
            txtCommitMessage.Enabled = !processing;
            btnRefreshBranches.Enabled = !processing;
            btnDownloadDocx.Enabled = !processing;
            cmbBranches.Enabled = !processing;

            btnProcess.Text = processing
                ? "Processing..."
                : "Normalize && Create PR";
            btnDownloadDocx.Text = processing
                ? "Processing..."
                : "Download && Save as DOCX";
        }
    }
}
