namespace CodeOwners.IO.Git
{
    public class GitHelper : IGitHelper
    {
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
