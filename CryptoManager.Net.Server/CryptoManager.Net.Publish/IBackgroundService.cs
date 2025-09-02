namespace CryptoManager.Net.Publish
{
    public interface IBackgroundService
    {
        Task ExecuteAsync(CancellationToken ct);
    }
}
