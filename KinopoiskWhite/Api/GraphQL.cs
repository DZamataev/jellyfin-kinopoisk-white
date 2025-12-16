using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace KinopoiskWhite.Api;
using Common;
using Models;

public class GraphQL
{
    public readonly HttpClient _client;
    private readonly TaskQueue _queue;
    private readonly JsonSerializerOptions _jsonOptions;

    public GraphQL(IHttpClientFactory httpClientFactory = null)
    {
        _queue = new TaskQueue();

        if (httpClientFactory == null)
            _client = new HttpClient();
        else
            _client = httpClientFactory.CreateClient();

        _client.BaseAddress = new System.Uri("https://graphql.kinopoisk.ru/");
        _client.DefaultRequestHeaders.Add("service-id", "25");

        _jsonOptions = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    }

    private static string GetEmbeddedQuery(string fileName)
    {
        var assembly = typeof(KinopoiskApi).Assembly;
        var resourceName = $"KinopoiskWhite.Api.Queries.{fileName}.gql";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) throw new FileNotFoundException($"Resource {resourceName} not found");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public async Task<JsonDocument> Call(string operationName, object variables, CancellationToken cancellationToken)
    {
        var query = GetEmbeddedQuery(operationName);
        var request = new { operationName, variables, query };
        var json = JsonSerializer.Serialize(request);

        var data = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await _queue.Enqueue( async () => {
            await Task.Delay(10); // minimal threshold

            return await _client.PostAsync("/graphql", data, cancellationToken);
        }).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync(cancellationToken);

        var doc = JsonDocument.Parse(result);

        if (doc.RootElement.ValueKind == JsonValueKind.Null)
            throw new System.Exception("Document is null");

        return doc;
    }

    public async Task<FilmInfo> Call(
        string operationName, object variables, string path,
        CancellationToken cancellationToken)
    {
        using var doc = await Call(operationName, variables, cancellationToken)
            .ConfigureAwait(false);

        var root = doc.RootElement;
        foreach (var chunk in path.Split('.'))
        {
            root = root.GetProperty(chunk);
        }
        return JsonSerializer.Deserialize<FilmInfo>(root, _jsonOptions);
    }

    public async Task<FilmInfo> SuggestSearch(string keyword, CancellationToken cancellationToken)
    => await Call(
        "SuggestSearch", new { keyword },
        "data.suggest.top.topResult.global", cancellationToken);

    public async Task<FilmInfo> FilmBaseInfo(int filmId, CancellationToken cancellationToken)
    => await Call(
        "FilmBaseInfo", new
        {
            filmId,
            isAuthorized = false,
            actorsLimit = 10,
            voiceOverActorsLimit = 0,
            relatedMoviesLimit = 0,
            checkSilentInvoiceAvailability = false,
            withPurchaseOptions = false,
            watchabilityLimit = 0,
            socialArgumentLimit = 0,
        },
        "data.film", cancellationToken);

    public async Task<FilmInfo> FilmPage(string contentUuid,
                                         CancellationToken cancellationToken,
                                         int seasonNumber = 0,
                                         int episodeNumber = 0)
    => await Call(
        "FilmPage",
        new { contentUuid, seasonNumber, episodeNumber, isAuthorized = false },
        "data.movieByContentUuid", cancellationToken);

    public async Task<FilmInfo> MovieImagesItems(int id, string type, CancellationToken cancellationToken)
    {
        // COVER, SHOOTING, STILL, POSTER, FAN_ART, PROMO, CONCEPT, WALLPAPER, SCREENSHOT

        var request = new
        {
            id,
            type,
            offset = 0,
            limit = 10
        };
        using var doc = await Call("MovieImagesItems", request, cancellationToken)
            .ConfigureAwait(false);

        var root = doc.RootElement
            .GetProperty("data")
            .GetProperty("movie");

        return JsonSerializer.Deserialize<FilmInfo>(root, _jsonOptions);
    }
}