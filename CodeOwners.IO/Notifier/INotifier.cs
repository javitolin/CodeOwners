using CodeOwners.Entities;

namespace CodeOwners.IO.Notifier
{
    public interface INotifier
    {
        void Notify(OwnerChangedFiles ownerChangedFiles, string message);
    }
}
