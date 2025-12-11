using System.Reflection;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.Movies;

namespace Jellyfin.Plugin.KinopoiskWhite; 

class Program {
    void checkQueue() {
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
    }

    async static Task FilmBaseInfo() {
        var request = new {
            filmId = 361,
            isAuthorized = false,
            actorsLimit = 10,
            voiceOverActorsLimit = 0,
            relatedMoviesLimit = 0,
            checkSilentInvoiceAvailability = false,
            withPurchaseOptions = false,
            watchabilityLimit = 0,
            socialArgumentLimit = 0,
        };
        var result = await Api.Instance.Call("FilmBaseInfo", request);
        var movie = new Movie();
        movie.GetFullInfo(result);

        Console.WriteLine($"Result: {movie}");
        // var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
        // Console.Write(System.Text.Json.JsonSerializer.Serialize(info, options));
        // DumpObject(movie);
    }

    static async Task Main(string[] args) {
        Console.WriteLine("Started");
        _ = await Api.Instance.GetMovie("Pulp.Fiction.1994.1080p.BrRip.x264.YIFY.mp4");
        Console.WriteLine("Finished");
    }
}