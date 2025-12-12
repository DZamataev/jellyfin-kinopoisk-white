using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.KinopoiskWhite;


public class KinopoiskApi
{
    public readonly HttpClient _client;
    private readonly TaskQueue _queue;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public KinopoiskApi(ILogger<KinopoiskApi> logger, IHttpClientFactory httpClientFactory)
    {
        _queue = new TaskQueue();

        _logger = logger;

        _client = httpClientFactory.CreateClient();
        _client.BaseAddress = new System.Uri("https://graphql.kinopoisk.ru/");
        _client.DefaultRequestHeaders.Add("service-id", "25");

        _jsonOptions = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    }

    public static string GetEmbeddedQuery(string fileName)
    {
        var assembly = typeof(KinopoiskApi).Assembly;
        var resourceName = $"Plugin.GraphQL.{fileName}.gql";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) throw new FileNotFoundException($"Resource {resourceName} not found");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public async Task<string> Call(string operationName, object variables, CancellationToken cancellationToken)
    {
        var query = GetEmbeddedQuery(operationName);
        var request = new { operationName, variables, query };
        var json = JsonSerializer.Serialize(request);

        var data = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/graphql", data, cancellationToken)
                .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<Film> SuggestSearch(string keyword, CancellationToken cancellationToken)
    {
        try
        {
            var result = await Call("SuggestSearch", new { keyword }, cancellationToken)
                .ConfigureAwait(false);

            using var doc = JsonDocument.Parse(result);
            var root = doc.RootElement
                .GetProperty("data")
                .GetProperty("suggest")
                .GetProperty("top")
                .GetProperty("topResult")
                .GetProperty("global");

            var film = JsonSerializer.Deserialize<Film>(root, _jsonOptions);
            _logger.LogInformation("SuggestSearch [{keyword}] found KID {kid}.", keyword, film.Id);
            return film;
        }
        catch (System.Exception ex)
        {
            _logger.LogDebug("SuggestSearch [{keyword}] not found.", keyword);
            _logger.LogTrace(ex, "Suggest Search");
        }
        return null;
    }

    public async Task<string> GetKinopoiskId(ItemLookupInfo info, CancellationToken cancellationToken)
    {
        var keywords = info.Path.ParseFileName();

        foreach (var (title, year) in keywords)
        {
            string keyword = (year == null) ? title : $"{title} {year}";

            var film = await _queue.Enqueue(async () =>
                await SuggestSearch(keyword, cancellationToken)
            ).ConfigureAwait(false);

            if (film?.Id != null)
                return System.Convert.ToString(film.Id);
        }
        throw new System.Exception($"Get Kinopoisk Id failed [{info.Name}].\n{keywords}");
    }

    public async Task<Film> FilmBaseInfo(int filmId, CancellationToken cancellationToken)
    {
        try
        {
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
            var result = await Call("FilmBaseInfo", request, cancellationToken);

            using var doc = JsonDocument.Parse(result);

            if (doc.RootElement.ValueKind == JsonValueKind.Null)
                throw new System.Exception("Document is null");

            var root = doc.RootElement
                .GetProperty("data")
                .GetProperty("film");

            var film = JsonSerializer.Deserialize<Film>(root, _jsonOptions);
            _logger.LogInformation("FilmBaseInfo for KID {kid} loaded.", filmId);
            return film;
        }
        catch (System.Exception)
        {
            _logger.LogDebug("FilmBaseInfo KID {kid} not found.", filmId);
        }
        return null;
    }

    public async Task Fetch<T>(MetadataResult<T> itemResult, string kinopoiskId, string language, string country, CancellationToken cancellationToken)
            where T : BaseItem
    {
        if (string.IsNullOrWhiteSpace(kinopoiskId))
        {
            throw new System.ArgumentNullException(nameof(kinopoiskId));
        }

        var kid = System.Convert.ToInt32(kinopoiskId);
        var film = await _queue.Enqueue(async () => await FilmBaseInfo(kid, cancellationToken));
        
        if (film != null)
        {
            film.Fill(itemResult.Item);
            return;
        }

        throw new System.Exception(
            $"Get Kinopoisk metadata failed KID {kinopoiskId} [{itemResult.Item.Name}].");
    }
}