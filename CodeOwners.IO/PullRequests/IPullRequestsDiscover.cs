using CodeOwners.Entities;

namespace CodeOwners.IO.PullRequests
{
    public interface IPullRequestsDiscover
    {
        public Task<IEnumerable<PR>> GetPRsAsync(CancellationToken cancellationToken);

        public Task<IEnumerable<string>> SetReviewersAsync(PR pr, OwnerChangedFiles ownerChangedFiles, CancellationToken cancellationToken);
    }
}
