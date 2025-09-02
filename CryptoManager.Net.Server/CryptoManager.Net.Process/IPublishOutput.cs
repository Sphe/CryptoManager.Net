namespace CryptoExchange.Net.Tracker.Publish
{
    public interface IPublishOutput<T>
    {
        Task PublishAsync(PublishItem<T> item);
    }
}
