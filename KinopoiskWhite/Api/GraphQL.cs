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
    IAsyncEnumerable<T> SuggestSearch<T>(string keyword, CancellationToken cancellationToken)
    where T : BaseMetadata, new();
    Task<T> CallAndDeserialize<T>(
        string operationName, object variables, string path,
        CancellationToken cancellationToken);
    Task<T> CallAndDeserializeApi<T>(string path, CancellationToken cancellationToken);
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

    // A null anywhere along the path — including at the leaf — is reported as
    // ElementIsNull. Checking only before GetProperty let a null leaf through as a
    // JsonValueKind.Null element, which callers then dereferenced: the KinoPoisk API
    // returns `data.suggest.top = null` (with an "Internal server error" in `errors`)
    // for a large share of SuggestSearch calls, and that null surfaced as an opaque
    // InvalidOperationException from JsonDocument.TryGetNamedPropertyValue.
    protected static JsonElement Walk(JsonElement root, string path)
    {
        foreach (var chunk in path.Split('.'))
        {
            if (root.ValueKind == JsonValueKind.Null)
                throw new Error.ElementIsNull();

            root = root.GetProperty(chunk);
        }

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

    // "Not found" and "the server faulted" are different: a null payload for a fetch
    // by id legitimately means no such item, and callers (WithCache) turn that null
    // into Error.GettingRemote. Keep that contract while Walk stays strict.
    public async Task<T> CallAndDeserialize<T>(
        string operationName, object variables, string path,
        CancellationToken cancellationToken)
    {
        JsonElement root;
        try
        {
            root = await Call(operationName, variables, path, cancellationToken);
        }
        catch (Error.ElementIsNull)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(root, _jsonOptions);
    }

    // yandexCityId is required in practice: without it the API answers
    // `{"errors":[{"message":"Internal server error","path":["suggest","top"]}]}` and
    // `data.suggest.top = null` for roughly half of all calls (measured from the
    // Jellyfin host: 12/20 null without it, 0/20 with it). 213 is Moscow — the value
    // the site's own web client sends by default.
    public const int DefaultYandexCityId = 213;

    public async IAsyncEnumerable<T>
    SuggestSearch<T>(string keyword, [EnumeratorCancellation] CancellationToken cancellationToken)
    where T : BaseMetadata, new()
    {
        var root = await Call(
            "SuggestSearch",
            new { keyword, limit = 10, yandexCityId = DefaultYandexCityId },
            "data.suggest.top",
            cancellationToken
        ).ConfigureAwait(false);

        T result = null;
        try
        {
            var top = Walk(root, "topResult.global");
            result = JsonSerializer.Deserialize<T>(top, _jsonOptions);
        }
        catch { }

        if (result != null) yield return result;
        else result = new();

        var rootPath = result.GetRootPath();
        var itemPath = result.GetItemPath();

        if (rootPath == null || itemPath == null) yield break;
        if (!root.TryGetProperty(rootPath, out var items)
            || items.ValueKind != JsonValueKind.Array) yield break;

        foreach (var item in items.EnumerateArray())
            if (item.TryGetProperty(itemPath, out var element))
                yield return JsonSerializer.Deserialize<T>(element, _jsonOptions);
    }

    public async Task<T> CallAndDeserializeApi<T>(
        string path,
        CancellationToken cancellationToken)
    {
        var root = await CallApi(path, cancellationToken);
        return JsonSerializer.Deserialize<T>(root, _jsonOptions);
    }
}