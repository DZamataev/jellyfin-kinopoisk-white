using System.Threading.Tasks;

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

        static async Task Main(string[] args) {
            Console.WriteLine("Started");

            var variables = new {
                keyword = "fight club",
                yandexCityId = 10777,
                limit = 0
            };

            var operationName = "SuggestSearch";
            var query = Api.GetEmbeddedQuery(operationName);
            var request = new { operationName, variables, query };

            var json = System.Text.Json.JsonSerializer.Serialize(request);
            var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await Api.Instance._client.PostAsync("/graphql", content);
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();
            // Console.WriteLine(jsonString);

            var result = jsonString.GetShortInfo();

            Console.WriteLine($"Result: {result}");
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            Console.Write(System.Text.Json.JsonSerializer.Serialize(result, options));
            // Console.WriteLine(result.Data.Title);
            Console.WriteLine("Finished");
        }
    }
}