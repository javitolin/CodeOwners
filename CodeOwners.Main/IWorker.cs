namespace CodeOwners
{
    public interface IWorker
    {
        Task RunAsync(CancellationToken cancellationToken);
    }
}