using CodeOwners.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Graph.Client;
using Microsoft.VisualStudio.Services.Identity;
using Microsoft.VisualStudio.Services.Identity.Client;
using Microsoft.VisualStudio.Services.WebApi;

namespace CodeOwners.IO.PullRequests
{
    public class AdoPullRequestsDiscover : IPullRequestsDiscover, IDisposable
    {
        private VssConnection _connection;
        private GitHttpClient _gitClient;
        private GraphHttpClient _graphClient;
        private IdentityHttpClient _identityClient;
        private string? _projectName;
        private string? _repoName;
        private bool _useSshUrl;
        private string _branchPrefixRemove;
        private short _reviewerDefaultVote;
        private ILogger<AdoPullRequestsDiscover> _logger;

        public AdoPullRequestsDiscover(ILogger<AdoPullRequestsDiscover> logger, 
            IConfiguration configuration)
        {
            _logger = logger;

            var adoSection = configuration.GetSection("ado");
            if (adoSection is null) throw new ArgumentNullException("ado");

            var collectionUri = adoSection.GetValue<string>("collection_uri");
            var pat = adoSection.GetValue<string>("pat");
            if (collectionUri is null || pat is null)
            {
                _logger.LogError("'collection_url' or 'pat' is missing from the 'ado' configuration");
                throw new ArgumentException();
            }

            _projectName = adoSection.GetValue<string>("project_name");
            _repoName = adoSection.GetValue<string>("repo_name");
            _useSshUrl = adoSection.GetValue<bool>("use_ssh_url");

            if (_projectName is null || _repoName is null)
            {
                _logger.LogError("'_projectName' or '_repoName' is missing from the 'ado' configuration");
                throw new ArgumentException();
            }

            var pullRequestInfoSection = adoSection.GetSection("pr_info");
            _branchPrefixRemove = pullRequestInfoSection.GetValue<string>("branch_prefix_remove") ?? string.Empty;
            _reviewerDefaultVote = pullRequestInfoSection.GetValue<short>("reviewer_default_vote");

            var creds = new VssBasicCredential(string.Empty, pat);
            _connection = new VssConnection(new Uri(collectionUri), creds);
            _gitClient = _connection.GetClient<GitHttpClient>();
            _graphClient = _connection.GetClient<GraphHttpClient>();
            _identityClient = _connection.GetClient<IdentityHttpClient>();
        }

        public async Task<IEnumerable<PR>> GetPRsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Loading active Pull Requests");

            List<PR> prs = new List<PR>();

            var pullRequests = await _gitClient.GetPullRequestsAsync(_projectName, _repoName, new GitPullRequestSearchCriteria()
            {
                Status = PullRequestStatus.Active,
            }, cancellationToken: cancellationToken);

            _logger.LogDebug($"Found [{pullRequests.Count}] active pull requests");

            foreach (var pullRequest in pullRequests.Where(pr => pr.IsDraft.HasValue && pr.IsDraft.Value == false))
            {
                var repositoryId = pullRequest.Repository.Id;
                var repository = await _gitClient.GetRepositoryAsync(repositoryId, cancellationToken: cancellationToken);

                PR pr = await ParseResponseAsync(pullRequest, repository, cancellationToken);
                prs.Add(pr);
            }

            return prs;
        }

        private async Task<PR> ParseResponseAsync(GitPullRequest pullRequest, GitRepository repository, CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Parsing response for pull request: [{pullRequest.Title}]");
            IEnumerable<Reviewer> reviewers = await GetReviewersAsync(pullRequest, cancellationToken);
            var parsePullRequest = new PR()
            {
                Id = pullRequest.PullRequestId,
                Name = pullRequest.Title,
                Description = pullRequest.Description,
                Repository = _useSshUrl ? repository.SshUrl : repository.WebUrl,
                DestinationBranch = pullRequest.TargetRefName.Replace(_branchPrefixRemove, ""),
                SourceBranch = pullRequest.SourceRefName.Replace(_branchPrefixRemove, ""),
                Reviewers = reviewers,
                Url = pullRequest.Url
            };

            _logger.LogDebug($"Pull request parsed: [{parsePullRequest}]");

            return parsePullRequest;
        }

        private async Task<IEnumerable<Reviewer>> GetReviewersAsync(GitPullRequest pullRequest, CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Getting reviewers for pull request [{pullRequest.Title}]");
            List<Reviewer> reviewers = new List<Reviewer>();
            if (!pullRequest.Reviewers.Any())
            {
                _logger.LogDebug($"Pull request [{pullRequest.Title}] doesn't have any reviewers yet");
                return reviewers;
            }

            var reviewersGuid = pullRequest.Reviewers.Select(r => new Guid(r.Id)).ToList();
            _logger.LogDebug($"Searching for reviewers identities using guids: [{string.Join(", ", reviewersGuid)}]");

            var identities = await _identityClient.ReadIdentitiesAsync(reviewersGuid);
            if (identities is null)
            {
                throw new ArgumentNullException(nameof(identities));
            }

            foreach (var identity in identities)
            {
                if (identity is null)
                {
                    _logger.LogWarning("Found null identity");
                    continue;
                }

                await TryAddReviewerFromIdentity(reviewers, identity, cancellationToken);
            }

            return reviewers;
        }

        private async Task TryAddReviewerFromIdentity(List<Reviewer> reviewers, Identity identity, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug($"Getting user information for identity: [{identity.SubjectDescriptor}]");

                var user = await _graphClient.GetUserAsync(identity.SubjectDescriptor.ToString(), cancellationToken: cancellationToken);
                _logger.LogDebug($"Found user: [{user.MailAddress}]");

                var reviewer = new Reviewer
                {
                    Id = identity.Id,
                    UniqueName = user.MailAddress
                };

                reviewers.Add(reviewer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error trying to find user by identity [{identity.SubjectDescriptor}]");
            }
        }

        public async Task<IEnumerable<string>> SetReviewersAsync(PR pr, OwnerChangedFiles ownerChangedFiles, CancellationToken cancellationToken = default)
        {
            List<string> addedOwners = new List<string>();

            foreach (var owner in ownerChangedFiles.GetOwners())
            {
                _logger.LogDebug($"Searching for owner [{owner}] in pull request [{pr.Name}]");

                if (pr.Reviewers.Any(reviewer => reviewer.UniqueName.Equals(owner)))
                {
                    _logger.LogDebug($"Owner [{owner}] is already a reviewer");
                    continue; // Already a reviewer
                }

                try
                {
                    var identities = await _identityClient.ReadIdentitiesAsync(IdentitySearchFilter.MailAddress, owner);
                    if (identities.Count != 1)
                    {
                        _logger.LogError($"Found [{identities.Count}] identities when looking for [{owner}]");
                        continue;
                    }

                    var identity = identities.First();

                    IdentityRefWithVote identityRefWithVote = new IdentityRefWithVote
                    {
                        Vote = _reviewerDefaultVote
                    };

                    _logger.LogDebug($"Adding owner [{owner}] as reviewer with vote [{_reviewerDefaultVote}] to pull request [{pr.Name}]");

                    var response = await _gitClient.CreatePullRequestReviewerAsync(identityRefWithVote, _projectName,
                        _repoName, pr.Id, identity.Id.ToString(), cancellationToken: cancellationToken);

                    addedOwners.Add(response.UniqueName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Caught exception adding owner [{owner}] as reviewer to pull request [{pr.Name}]");
                }
            }

            return addedOwners;
        }

        public void Dispose()
        {
            _gitClient?.Dispose();
        }
    }
}
