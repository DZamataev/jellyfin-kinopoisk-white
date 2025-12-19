using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace KinopoiskWhite.Api;

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Common;
using Models;

public interface IGraphQL
{
    IAsyncEnumerable<FilmInfo> SuggestSearch(string keyword, CancellationToken cancellationToken);
    Task<FilmInfo> FilmBaseInfo(int filmId, CancellationToken cancellationToken);
    Task<FilmInfo> FilmPage(string contentUuid, CancellationToken cancellationToken, int seasonNumber = 0, int episodeNumber = 0);
    Task<FilmInfo> MovieImagesItems(int id, FilmImageType type, CancellationToken cancellationToken);
    Task<FilmPerson> GetPerson(int id, CancellationToken cancellationToken);
}

public class GraphQL : BaseSingleton, IGraphQL
{
    private readonly HttpClient _client;
    private readonly HttpClient _apiClient;
    private readonly TaskQueue _queue;
    private readonly JsonSerializerOptions _jsonOptions;

    public GraphQL(ILogger<GraphQL> logger, IHttpClientFactory httpClientFactory)
    : base(logger, httpClientFactory)
    {
        _queue = new TaskQueue();

        if (httpClientFactory == null)
            _client = new HttpClient();
        else
            _client = httpClientFactory.CreateClient();

        _client.DefaultRequestHeaders.Add("service-id", "25");

        _apiClient = new HttpClient(new HttpClientHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip |
                                     System.Net.DecompressionMethods.Deflate
        });
        _apiClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
        _apiClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    }

    private static string GetEmbeddedQuery(string fileName)
    {
        var assembly = typeof(GraphQL).Assembly;
        var resourceName = $"KinopoiskWhite.Api.Queries.{fileName}.gql";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) throw new FileNotFoundException($"Resource {resourceName} not found");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private async Task<JsonDocument> CallApi(string method, CancellationToken cancellationToken)
    {
        var url = $"https://www.kinopoisk.ru/api/{method}";
        _logger.LogTrace("{url}", url);

        var response = await _queue.Enqueue(async () =>
        {
            await Task.Delay(10); // minimal threshold

            return await _apiClient.GetAsync(url, cancellationToken);
        }).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync(cancellationToken);

        var doc = JsonDocument.Parse(result);

        if (doc.RootElement.ValueKind == JsonValueKind.Null)
            throw new System.Exception("Document is null");

        return doc;
    }

    private async Task<JsonElement> Call(string operationName, object variables, CancellationToken cancellationToken)
    {
        var url = "https://graphql.kinopoisk.ru/graphql";
        var query = GetEmbeddedQuery(operationName);
        var request = new { operationName, variables, query };
        var json = JsonSerializer.Serialize(request);

        var data = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        _logger.LogTrace("{url} {json}", url, json);

        var response = await _queue.Enqueue(async () =>
        {
            await Task.Delay(10); // minimal threshold

            return await _client.PostAsync(url, data, cancellationToken);
        }).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync(cancellationToken);

        using var doc = JsonDocument.Parse(result);

        if (doc.RootElement.ValueKind == JsonValueKind.Null)
            throw new System.Exception("Document is null");

        return doc.RootElement.Clone();
    }

    private static JsonElement Walk(JsonElement root, string path)
    {
        foreach (var chunk in path.Split('.'))
            root = root.GetProperty(chunk);

        return root;
    }

    private async Task<JsonElement> Call(
        string operationName, object variables, string path,
        CancellationToken cancellationToken)
    {
        var root = await Call(operationName, variables, cancellationToken)
            .ConfigureAwait(false);

        return Walk(root, path);
    }

    private async Task<FilmInfo> CallAndDeserialize(
        string operationName, object variables, string path,
        CancellationToken cancellationToken)
    {
        var root = await Call(operationName, variables, path, cancellationToken);
        return JsonSerializer.Deserialize<FilmInfo>(root, _jsonOptions);
    }

    public async IAsyncEnumerable<FilmInfo>
    SuggestSearch(string keyword, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var root = await Call(
            "SuggestSearch",
            new { keyword, limit = 10 },
            "data.suggest.top",
            cancellationToken
        ).ConfigureAwait(false);

        var top = root.GetProperty("topResult").GetProperty("global");
        yield return JsonSerializer.Deserialize<FilmInfo>(top, _jsonOptions);

        foreach (var movie in root.GetProperty("movies").EnumerateArray())
            yield return JsonSerializer.Deserialize<FilmInfo>(
                movie.GetProperty("movie"), _jsonOptions);
    }

    public async Task<FilmInfo> FilmBaseInfo(int filmId, CancellationToken cancellationToken)
    => await CallAndDeserialize(
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
    => await CallAndDeserialize(
        "FilmPage",
        new { contentUuid, seasonNumber, episodeNumber, isAuthorized = false },
        "data.movieByContentUuid", cancellationToken);

    public async Task<FilmInfo>
    MovieImagesItems(int id, FilmImageType type, CancellationToken cancellationToken)
     => await CallAndDeserialize("MovieImagesItems", new { id, type, offset = 0, limit = 50 },
                   "data.movie", cancellationToken);

    public async Task<FilmPerson>
    GetPerson(int id, CancellationToken cancellationToken)
    {
        var root = await CallApi($"person/{id}", cancellationToken);
        return JsonSerializer.Deserialize<FilmPerson>(root, _jsonOptions);
    }
}