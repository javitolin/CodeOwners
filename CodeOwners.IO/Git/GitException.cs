namespace CodeOwners.IO.Git
{
    public class GitException : InvalidOperationException
    {
        public GitException(int exitCode, string errors) : base(errors) =>
            this.ExitCode = exitCode;

        /// <summary>
        /// The exit code returned when running the Git command.
        /// </summary>
        public readonly int ExitCode;
    }
}
