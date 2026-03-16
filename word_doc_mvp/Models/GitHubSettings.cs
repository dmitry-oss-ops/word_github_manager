using System.Configuration;

namespace word_doc_mvp.Models
{
    public class GitHubSettings
    {
        public string Username { get; set; }
        public string Token { get; set; }
        public string RepositoryName { get; set; }

        public static GitHubSettings LoadFromConfig()
        {
            return new GitHubSettings
            {
                Username = ConfigurationManager.AppSettings["GitHubUsername"] ?? "",
                Token = ConfigurationManager.AppSettings["GitHubToken"] ?? "",
                RepositoryName = ConfigurationManager.AppSettings["GitHubRepo"] ?? "docx-version-control"
            };
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Username)
                && !string.IsNullOrWhiteSpace(Token)
                && !string.IsNullOrWhiteSpace(RepositoryName);
        }
    }
}
