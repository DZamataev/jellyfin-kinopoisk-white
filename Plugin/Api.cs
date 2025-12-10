using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.RegularExpressions;
using MediaBrowser.Controller.Entities.Movies;

namespace Jellyfin.Plugin.KinopoiskWhite {
    public class Api {
        private static readonly System.Lazy<Api> _instance = new System.Lazy<Api>(() => new Api());
        public static Api Instance => _instance.Value;

        public readonly HttpClient _client;
        private readonly TaskQueue _queue;

        private Api() {
            _queue = new TaskQueue();
            _client = new HttpClient();
            _client.BaseAddress = new System.Uri("https://graphql.kinopoisk.ru/");
            _client.DefaultRequestHeaders.Add("service-id", "25");
        }

        public (string, int?) ParseFileName(string path) {
            var fileName = Path.GetFileName(path);
            var parts = Regex.Split(fileName, @"((?:19|20)\d{2})");
            
            int drop = parts.Length;
            int? year = null;
            
            for (int i = parts.Length - 1; i >= 0; i--) {
                drop--;
                if (Regex.IsMatch(parts[i], @"^\d{4}$")) {
                    year = int.Parse(parts[i]);
                    break;
                }
            }
            
            var result = string.Join(" ", parts.Take(drop));
            
            // no match
            if (string.IsNullOrEmpty(result)) {
                result = Regex.Replace(fileName, @"\.\w+$", "");
            }
            // cleanup
            result = Regex.Replace(result, @"[^\w]+|[_\s]", " ").Trim();

            return (result, year);
        }

        public static string GetEmbeddedQuery(string fileName) {
            var assembly = typeof(Api).Assembly;
            var resourceName = $"Plugin.GraphQL.{fileName}.gql";
            
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) throw new FileNotFoundException($"Resource {resourceName} not found");
            
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public async Task<string> Call(string operationName, object variables) {
            var query = GetEmbeddedQuery(operationName);
            var request = new { operationName, variables, query };
            var json = JsonSerializer.Serialize(request);

            var data = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/graphql", data);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<int?> SuggestSearch(Movie movie, string keyword) {
            var request = new {
                keyword,
                yandexCityId = 10777,
                limit = 0
            };
            var result = await Call("SuggestSearch", request);
            return movie.GetShortInfo(result);
        }

        public async Task FilmBaseInfo(Movie movie, int filmId) {
            var request = new {
                filmId,
                isAuthorized = false,
                actorsLimit = 10,
                voiceOverActorsLimit = 0,
                relatedMoviesLimit = 0,
                checkSilentInvoiceAvailability = false,
                withPurchaseOptions = false,
                watchabilityLimit = 0,
                socialArgumentLimit = 0,
            };
            var result = await Call("FilmBaseInfo", request);
            movie.GetFullInfo(result);
        }

        public async Task<Movie> GetMovie(string path) {
            var (title, year) = ParseFileName(path);
            var movie = new Movie {
                Id = System.Guid.NewGuid(),
                DateCreated = System.DateTime.UtcNow,
                Name = title,
                ProductionYear = year,
                Overview = "Test description с кириллицей",
            };

            string keyword = (year == null) ? title : $"{title} {year}";

            return await _queue.Enqueue(async () => {
                var kid = await SuggestSearch(movie, keyword);
                if (kid.HasValue) {
                    await Task.Delay(100);
                    try {
                        await FilmBaseInfo(movie, kid.Value);
                        await Task.Delay(100);
                    } catch {}
                }
                return movie;
            });
        }
    }
}