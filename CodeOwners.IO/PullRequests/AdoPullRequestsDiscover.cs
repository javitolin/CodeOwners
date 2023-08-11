using CodeOwners.Entities;
using Microsoft.Extensions.Configuration;
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
        private const string BRANCH_DEFAULT_REMOVE = "refs/heads/";
        private const short VOTE_PENDING = -5;

        private VssConnection _connection;
        private GitHttpClient _gitClient;
        private GraphHttpClient _graphClient;
        private IdentityHttpClient _identityClient;
        private string? _projectName;
        private string? _repoName;
        private bool _useSshUrl;
        private Dictionary<string, GraphUser> _usersMapping = new Dictionary<string, GraphUser>();

        public AdoPullRequestsDiscover(IConfiguration configuration)
        {
            var adoSection = configuration.GetSection("ado");
            var collectionUri = adoSection.GetValue<string>("collection_uri");
            var pat = adoSection.GetValue<string>("pat");
            _projectName = adoSection.GetValue<string>("project_name");
            _repoName = adoSection.GetValue<string>("repo_name");
            _useSshUrl = adoSection.GetValue<bool>("use_ssh_url");

            var creds = new VssBasicCredential(string.Empty, pat);
            _connection = new VssConnection(new Uri(collectionUri), creds);
            _gitClient = _connection.GetClient<GitHttpClient>();
            _graphClient = _connection.GetClient<GraphHttpClient>();
            _identityClient = _connection.GetClient<IdentityHttpClient>();
        }

        public async Task<IEnumerable<PR>> GetPRsAsync(CancellationToken cancellationToken = default)
        {
            List<PR> prs = new List<PR>();


            var pullRequests = await _gitClient.GetPullRequestsAsync(_projectName, _repoName, new GitPullRequestSearchCriteria()
            {
                Status = PullRequestStatus.Active
            }, cancellationToken: cancellationToken);

            foreach (var pullRequest in pullRequests)
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
            IEnumerable<Reviewer> reviewers = await GetReviewersAsync(pullRequest, cancellationToken);
            return new PR()
            {
                Id = pullRequest.PullRequestId,
                Name = pullRequest.Title,
                Repository = _useSshUrl ? repository.SshUrl : repository.WebUrl,
                DestinationBranch = pullRequest.TargetRefName.Replace(BRANCH_DEFAULT_REMOVE, ""),
                SourceBranch = pullRequest.SourceRefName.Replace(BRANCH_DEFAULT_REMOVE, ""),
                Reviewers = reviewers,
                Url = pullRequest.Url
            };
        }

        private async Task<IEnumerable<Reviewer>> GetReviewersAsync(GitPullRequest pullRequest, CancellationToken cancellationToken)
        {
            List<Reviewer> reviewers = new List<Reviewer>();
            if (!pullRequest.Reviewers.Any()) return reviewers;

            var identities = await _identityClient.ReadIdentitiesAsync(pullRequest.Reviewers.Select(r => new Guid(r.Id)).ToList()); // await _identityClient.ReadIdentitiesAsync(IdentitySearchFilter.Identifier, prReviewer.Id, cancellationToken: cancellationToken);

            foreach (var identity in identities)
            {
                var user = await _graphClient.GetUserAsync(identity.SubjectDescriptor.ToString(), cancellationToken: cancellationToken);
                var reviewer = new Reviewer
                {
                    Id = identity.Id,
                    UniqueName = user.MailAddress
                };

                reviewers.Add(reviewer);
            }

            return reviewers;
        }

        public async Task<IEnumerable<string>> SetReviewersAsync(PR pr, OwnerChangedFiles ownerChangedFiles, CancellationToken cancellationToken = default)
        {
            List<string> addedOwners = new List<string>();

            foreach (var owner in ownerChangedFiles.GetOwners())
            {
                if (pr.Reviewers.Any(reviewer => reviewer.UniqueName.Equals(owner)))
                    continue; // Already a reviewer
                try
                {
                    var identities = await _identityClient.ReadIdentitiesAsync(IdentitySearchFilter.MailAddress, owner);
                    if (identities.Count != 1)
                    {
                        // TODO Log error
                        continue;
                    }

                    var identity = identities.First();

                    IdentityRefWithVote identityRefWithVote = new IdentityRefWithVote
                    {
                        Vote = VOTE_PENDING
                    };

                    var response = await _gitClient.CreatePullRequestReviewerAsync(identityRefWithVote, _projectName,
                        _repoName, pr.Id, identity.Id.ToString(), cancellationToken: cancellationToken);

                    addedOwners.Add(response.UniqueName);
                }
                catch (Exception ex)
                {

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
