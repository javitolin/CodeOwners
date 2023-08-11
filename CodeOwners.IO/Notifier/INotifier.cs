using CodeOwners.Entities;

namespace CodeOwners.IO.Notifier
{
    public interface INotifier
    {
        Task NotifyAsync(PR pullRequest, IEnumerable<string> usersToNotify, CancellationToken cancellationToken);
    }
}
