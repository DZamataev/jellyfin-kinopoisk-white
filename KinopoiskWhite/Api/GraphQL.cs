using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;

namespace KinopoiskWhite.Api;

using Common;
using Models;
using Providers;

public interface IGraphQL
{
    Task<FilmInfo> CallAndDeserialize(
        string operationName, object variables, string path,
        CancellationToken cancellationToken);
    IAsyncEnumerable<T> SuggestSearch<T>(string keyword, CancellationToken cancellationToken)
    where T : BaseMetadata;

    Task<FilmInfo> FilmBaseInfo(int filmId, CancellationToken cancellationToken);
    Task<FilmInfo> FilmPage(string contentUuid, CancellationToken cancellationToken, int seasonNumber = 0, int episodeNumber = 0);
    Task<FilmInfo> MovieImagesItems(int id, FilmImageType type, CancellationToken cancellationToken);
    Task<FilmPerson> GetPerson(int id, CancellationToken cancellationToken);
}

public class GraphQL : BaseSingleton, IGraphQL
{
    public new class Error(string message) : Base.Error(message)
    {
        public class NullResponse(string message): Base.Error(message) {}
        public class ElementIsNull(): NullResponse("Element is null") {}
        public class DocumentIsNull(): NullResponse("Document is null") {}
        public class DocumentInvalid(): Base.Error("Invalid document") {}
    }

    private readonly HttpClient _client;
    private readonly HttpClient _apiClient;
    private readonly TaskQueue _queue;
    private readonly JsonSerializerOptions _jsonOptions;

    public class Registrator : IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection services, IServerApplicationHost applicationHost)
        {
            services.AddHttpClient("GqlClient", client =>
            {
                client.DefaultRequestHeaders.Add("service-id", "25");
            });

            services.AddHttpClient("ApiClient", client =>
            {
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
                client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            })
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                {
                    AutomaticDecompression = System.Net.DecompressionMethods.GZip |
                                            System.Net.DecompressionMethods.Deflate
                });

            services.AddSingleton<IGraphQL, GraphQL>();
        }
    }

    public GraphQL(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
    : base(loggerFactory)
    {
        _queue = new TaskQueue();

        _client = httpClientFactory.CreateClient("GqlClient");
        _apiClient = httpClientFactory.CreateClient("ApiClient");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    }

    protected static string GetEmbeddedQuery(string fileName)
    {
        var assembly = typeof(GraphQL).Assembly;
        var resourceName = $"KinopoiskWhite.Api.Queries.{fileName}.gql";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) throw new FileNotFoundException($"Resource {resourceName} not found");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    protected static async Task<JsonElement> Parse(HttpResponseMessage response,
                                                   CancellationToken cancellationToken)
    {
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(result))
            throw new Error.DocumentIsNull();

        try
        {
            return JsonDocument.Parse(result).RootElement.Clone();
        }
        catch (JsonException)
        {
            throw new Error.DocumentInvalid();
        }
    }

    protected async Task<JsonElement> CallApi(string method, CancellationToken cancellationToken)
    {
        var url = $"https://www.kinopoisk.ru/api/{method}";
        _logger.LogTrace("{url}", url);

        var response = await _queue.Enqueue(async () =>
        {
            await Task.Delay(10); // minimal threshold

            return await _apiClient.GetAsync(url, cancellationToken);
        }).ConfigureAwait(false);

        return await Parse(response, cancellationToken);
    }

    protected async Task<JsonElement> Call(string operationName, object variables, CancellationToken cancellationToken)
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

        return await Parse(response, cancellationToken);
    }

    protected static JsonElement Walk(JsonElement root, string path)
    {
        foreach (var chunk in path.Split('.'))
            root = root.GetProperty(chunk);

        if (root.ValueKind == JsonValueKind.Null)
            throw new Error.ElementIsNull();

        return root;
    }

    protected async Task<JsonElement> Call(
        string operationName, object variables, string path,
        CancellationToken cancellationToken)
    {
        var root = await Call(operationName, variables, cancellationToken)
            .ConfigureAwait(false);

        return Walk(root, path);
    }

    public async Task<FilmInfo> CallAndDeserialize(
        string operationName, object variables, string path,
        CancellationToken cancellationToken)
    {
        var root = await Call(operationName, variables, path, cancellationToken);
        return JsonSerializer.Deserialize<FilmInfo>(root, _jsonOptions);
    }

    public async IAsyncEnumerable<T>
    SuggestSearch<T>(string keyword, [EnumeratorCancellation] CancellationToken cancellationToken)
    where T : BaseMetadata
    {
        var root = await Call(
            "SuggestSearch",
            new { keyword, limit = 10 },
            "data.suggest.top",
            cancellationToken
        ).ConfigureAwait(false);

        var top = root.GetProperty("topResult").GetProperty("global");

        T result = default(T);
        try
        {
            result = JsonSerializer.Deserialize<T>(top, _jsonOptions);
        }
        catch { }

        if (result != null) yield return result;

        foreach (var item in root.GetProperty(result.GetRootPath()).EnumerateArray())
            yield return JsonSerializer.Deserialize<T>(
                item.GetProperty(result.GetItemPath()), _jsonOptions);
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

    public async Task<FilmInfo>
    FilmPage(string contentUuid, CancellationToken cancellationToken,
             int seasonNumber = 0, int episodeNumber = 0)
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