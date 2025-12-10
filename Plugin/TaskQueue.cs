using System.Threading.Tasks;
using System.Threading.Channels;

namespace Jellyfin.Plugin.KinopoiskWhite
{
    public class TaskQueue
    {
        private readonly Channel<System.Func<Task>> _channel;
        private readonly Task _processor;
        private System.Threading.CancellationTokenSource _cts = new();

        public TaskQueue(int capacity = 100) {
            _channel = Channel.CreateBounded<System.Func<Task>>(
                new BoundedChannelOptions(capacity));

            _processor = Task.Run(async () => {
                await foreach (var task in _channel.Reader.ReadAllAsync(_cts.Token)) {
                    await task();
                }
            });
        }

        public void Dispose() {
            _cts.Cancel();
            _channel.Writer.Complete();
            _processor.Wait();
        }

        public async Task<T> Enqueue<T>(System.Func<Task<T>> task) {
            var completion = new TaskCompletionSource<T>();
            var wrapper = async () => {
                try {
                    var result = await task();
                    completion.SetResult(result);
                } catch (System.Exception ex) {
                    completion.SetException(ex);
                }
            };
            await _channel.Writer.WriteAsync(wrapper, _cts.Token);
            return await completion.Task;
        }
    }
}