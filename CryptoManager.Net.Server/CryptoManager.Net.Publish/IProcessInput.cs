namespace CryptoManager.Net.Publish
{
    public interface IProcessInput<T>
    {
        Task<PublishItem<T>?> ReadAsync(CancellationToken ct);
    }
}
