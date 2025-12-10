using System.Threading.Tasks;
using Jellyfin.Plugin.KinopoiskWhite;

Console.WriteLine("Started");

var queue = new TaskQueue();
List<Task> tasks = [];
for (var i = 0; i < 10; i++) {
    var value = i;
    var j = Task.Run(async () => {
        var result = await queue.Enqueue(async () => {
            await Task.Delay(250);
            return value;
        });
        Console.WriteLine($"Result: {result}");
    });
    tasks.Add(j);
}

Task.WaitAll(tasks.ToArray());
Console.WriteLine("Finished");