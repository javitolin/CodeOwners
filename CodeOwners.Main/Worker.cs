using CodeOwners.Entities;
using CodeOwners.IO.CodeOwnerFinder;
using CodeOwners.IO.Git;
using CodeOwners.IO.Notifier;
using CodeOwners.IO.Parser;
using CodeOwners.IO.PullRequests;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Common;

namespace CodeOwners
{
    public class Worker : IWorker
    {
        private ILogger<Worker> _logger;
        IPullRequestsDiscover _pullRequestsDiscoverer;
        IGitHelper _gitHelper;
        ICodeOwnersParser _codeOwnersParser;
        ICodeOwnersFinder _codeOwnersFinder;
        ANotifier _notifier;

        public Worker(ILogger<Worker> logger, IPullRequestsDiscover pRDiscover, IGitHelper gitHelper, ICodeOwnersParser codeOwnersParser, ICodeOwnersFinder codeOwnerFinder, ANotifier notifier)
        {
            _logger = logger;
            _pullRequestsDiscoverer = pRDiscover;
            _gitHelper = gitHelper;
            _codeOwnersParser = codeOwnersParser;
            _codeOwnersFinder = codeOwnerFinder;
            _notifier = notifier;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker starting");

            var pullRequests = await _pullRequestsDiscoverer.GetPRsAsync(cancellationToken);

            foreach (var pullRequest in pullRequests)
            {
                // Use temporary directory
                var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(tempDir);
                _logger.LogInformation($"Using directory [{tempDir}]");

                // Clone the repository
                _logger.LogInformation($"Cloning repository [{pullRequest.Repository}], branch: [{pullRequest.DestinationBranch}]");
                _gitHelper.Clone(tempDir, pullRequest.Repository, pullRequest.DestinationBranch);

                // Parse CODEOWNERS file from destination branch
                _logger.LogInformation($"Parsing CodeOwners file");
                var codeOwnerMap = _codeOwnersParser.Parse(tempDir);
                if (codeOwnerMap is null)
                {
                    continue;
                }

                // Checkout the source branch from the pull request
                _logger.LogInformation($"Checking out source branch [{pullRequest.SourceBranch}]");
                _gitHelper.Checkout(tempDir, pullRequest.SourceBranch);

                // Find the changed files and relevant owners
                _logger.LogInformation("Finding diffs between branches");
                var changes = _gitHelper.Diff(tempDir, pullRequest.SourceBranch, pullRequest.DestinationBranch);
                var ownersToNotify = _codeOwnersFinder.FindOwners(codeOwnerMap, changes);

                // Set owners as reviewers and notify them
                _logger.LogInformation($"Setting owners as reviewers and notifying");
                var addedOwners = await _pullRequestsDiscoverer.SetReviewersAsync(pullRequest, ownersToNotify, cancellationToken);
                await _notifier.NotifyAsync(pullRequest, addedOwners, cancellationToken);
            }
            _logger.LogInformation("Worker finished");
        }
    }
}
