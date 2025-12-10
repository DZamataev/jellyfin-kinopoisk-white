using System.Reflection;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.Movies;

namespace Jellyfin.Plugin.KinopoiskWhite {
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
            DumpObject(movie);
        }

        static async Task Main(string[] args) {
            Console.WriteLine("Started");
            var movie = await Api.Instance.GetMovie("Pulp.Fiction.1994.1080p.BrRip.x264.YIFY.mp4");
            DumpObject(movie);
            Console.WriteLine("Finished");
        }

        public static void DumpObject(object obj)
        {
            if (obj == null)
            {
                Console.WriteLine("Object: null");
                return;
            }

            Type type = obj.GetType();
            Console.WriteLine($"Type: {type.FullName}");

            foreach (PropertyInfo prop in type.GetProperties(
                // BindingFlags.Public |
                // BindingFlags.Instance |
                // BindingFlags.DeclaredOnly
                ))
            {
                try
                {
                    // Получаем значение
                    object? value = prop.GetValue(obj);

                    if (value == null) continue;

                    Console.WriteLine($"{prop.Name}: {value}");
                }
                catch (Exception)
                {
                    // Console.WriteLine($"{prop.Name}: ERROR - {ex.GetType().Name}: {ex.Message}");
                }
            }
        }
    }
}