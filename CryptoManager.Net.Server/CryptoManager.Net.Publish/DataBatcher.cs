using System.Collections.Concurrent;

namespace CryptoManager.Net.Data
{
    public class DataBatcher<T>
    {
        private readonly ConcurrentDictionary<string, T> _store = new ConcurrentDictionary<string, T>();
        private readonly Func<Dictionary<string, T>, Task> _callback;
        private readonly Func<T, T, T> _updateFunc;
        private readonly SemaphoreSlim _sem = new SemaphoreSlim(1, 1);
        private readonly TimeSpan _triggerInterval;
        private Task _processingTask = Task.CompletedTask;
        private readonly Lock _locker = new Lock();
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private ManualResetEvent _triggerEvent = new ManualResetEvent(false);

        public DataBatcher(TimeSpan triggerInterval, Func<Dictionary<string, T>, Task> callback, Func<T, T, T>? updateFunc = null)
        {
            _triggerInterval = triggerInterval;
            _callback = callback;
            _updateFunc = updateFunc ?? ((xExisting, xUpdate) => xUpdate);
        }

        public Task StartAsync()
        {
            _processingTask = Task.Run(ProcessAsync);
            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            _cts.Cancel();
            await _processingTask;
        }

        public async Task AddAsync(Dictionary<string, T> data)
        {
            await _sem.WaitAsync();
            try
            {
                lock (_locker)
                {
                    foreach (var item in data)
                    {
                        if (_store.TryGetValue(item.Key, out var existing))
                            _store[item.Key] = _updateFunc(existing, item.Value);
                        else
                            _store[item.Key] = item.Value;
                    }
                }

                _triggerEvent.Set();
            }
            finally
            {
                _sem.Release();
            }
        }

        private async Task ProcessAsync()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    _triggerEvent.WaitOne();
                    await Task.Delay(_triggerInterval);

                    Dictionary<string, T> data;
                    lock (_locker)
                    {
                        data = _store.ToDictionary(x => x.Key, x => x.Value);
                        _store.Clear();
                    }
                    _triggerEvent.Reset();

                    if (data.Any())
                        await _callback(data);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
