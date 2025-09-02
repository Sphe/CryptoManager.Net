namespace CryptoManager.Net.Publish
{
    public interface IPublishOutput<T>
    {
        Task PublishAsync(PublishItem<T> item);
    }
}
