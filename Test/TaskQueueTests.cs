using Xunit;

using KinopoiskWhite.Common;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using System.Threading.Channels;

namespace Test; 


class TaskQueueTestsException() : System.Exception("Task queue test exception") {}

class MockTaskQueue: TaskQueue
{
    public Channel<System.Func<Task>> Channel => _channel;
    public Task Processor => _processor;
    public CancellationTokenSource Cts => _cts;
}

public class TaskQueueTests {
    [Fact]
    public async Task ShouldRunSequently()
    {
        TaskQueue queue = new();

        var values = Enumerable.Range(0, 10).Reverse().ToArray();

        var jobs = values.Select(n =>
            Task.Run(async () =>
                await queue.Enqueue(async () =>
                {
                    await Task.Delay(n * 10);
                    return n;
                })
            )
        ).ToArray();

        var result = await Task.WhenAll(jobs);

        Assert.True(Enumerable.SequenceEqual(values, result));
    }

    [Fact]
    public async Task ShouldThrowAnException()
    {
        TaskQueue queue = new();

        _ = await Assert.ThrowsAsync<TaskQueueTestsException>(async () =>
            await queue.Enqueue<int>(async () =>
            {
                await Task.Delay(1);
                throw new TaskQueueTestsException();
            }));
    }

    [Fact]
    public void DisposeShouldAllowGarbageCollection()
    {
        MockTaskQueue queue = new();
        var weakRef = new System.WeakReference<MockTaskQueue>(queue);

        var cts = queue.Cts;
        var channel = queue.Channel;
        var processor = queue.Processor;

        Assert.Throws<System.AggregateException>(() => queue.Dispose());

        Assert.True(cts.IsCancellationRequested);
        Assert.True(processor.IsCanceled);
        Assert.True(processor.IsCompleted);
        Assert.False(channel.Writer.TryComplete());
    }
}