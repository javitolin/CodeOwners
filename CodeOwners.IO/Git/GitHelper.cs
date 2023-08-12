using Microsoft.Extensions.Configuration;

namespace CodeOwners.IO.Git
{
    public class GitHelper : IGitHelper
    {
        private string _authorizationHeader;
        public GitHelper(IConfiguration configuration)
        {
            var pat = configuration.GetSection("ado").GetValue<string>("pat");
            var authorizationPat = Base64Encode($":{pat}");

            _authorizationHeader = $" -c http.extraheader=\"Authorization: Basic {authorizationPat}\" ";
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public string Checkout(string directory, string branch)
        {
            var response = Run(directory, $"checkout {branch}");
            return response;
        }

        public IEnumerable<string> Diff(string directory, string firstBranch, string secondBranch)
        {
            var response = Run(directory, $"diff --name-only {firstBranch}..{secondBranch}");

            return response.Split(Environment.NewLine).ToList();
        }

        public string Clone(string directory, string repository, string branch)
        {
            var response = Run(directory, $"clone {repository} .");
            response += Checkout(directory, branch);
            return response;
        }

        private string Run(string directory, string arguments)
        {
            using var process = new System.Diagnostics.Process();

            arguments = _authorizationHeader + arguments;
            var exitCode = process.Run(@"git", arguments, directory,
                out var output, out var errors);

            if (exitCode == 0)
            {
                return output;
            }

            throw new GitException(exitCode, errors);

        }
    }
}
