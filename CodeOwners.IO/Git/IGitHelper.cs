namespace CodeOwners.IO.Git
{
    public interface IGitHelper
    {
        string Clone(string directory, string repository, string branch);

        string Checkout(string directory, string branch);

        IEnumerable<string> Diff(string directory, string firstBranch, string secondBranch);
    }
}
