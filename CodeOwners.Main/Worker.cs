using CodeOwners.Entities;
using CodeOwners.IO.CodeOwnerFinder;
using CodeOwners.IO.Git;
using CodeOwners.IO.Parser;
using CodeOwners.IO.PullRequests;

namespace CodeOwners
{
    public class Worker : IWorker
    {
        IPullRequestsDiscover _pullRequestsDiscoverer;
        IGitHelper _gitHelper;
        ICodeOwnersParser _codeOwnersParser;
        ICodeOwnersFinder _codeOwnersFinder;

        public Worker(IPullRequestsDiscover pRDiscover, IGitHelper gitHelper, ICodeOwnersParser codeOwnersParser, ICodeOwnersFinder codeOwnerFinder)
        {
            _pullRequestsDiscoverer = pRDiscover;
            _gitHelper = gitHelper;
            _codeOwnersParser = codeOwnersParser;
            _codeOwnersFinder = codeOwnerFinder;
            //_notifyOwner = notifyOwner;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var pullRequests = await _pullRequestsDiscoverer.GetPRsAsync(cancellationToken);

            foreach (var pullRequest in pullRequests)
            {
                // Use temporary directory
                var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(tempDir);

                // Clone the repository
                _gitHelper.Clone(tempDir, pullRequest.Repository, pullRequest.DestinationBranch);

                // Parse CODEOWNERS file from destination branch
                var codeownersFilename = Path.Combine(tempDir, Constants.CODEOWNERS_FILENAME);
                var codeOwnerMap = _codeOwnersParser.Parse(codeownersFilename);

                // Checkout the source branch from the pull request
                _gitHelper.Checkout(tempDir, pullRequest.SourceBranch);

                // Find the changed files and relevant owners
                var changes = _gitHelper.Diff(tempDir, pullRequest.SourceBranch, pullRequest.DestinationBranch);
                var ownersToNotify = _codeOwnersFinder.FindOwners(codeOwnerMap, changes);

                // Set owners as reviewers and notify them
                await _pullRequestsDiscoverer.SetReviewersAsync(pullRequest, ownersToNotify, cancellationToken);
            }
        }
    }
}
