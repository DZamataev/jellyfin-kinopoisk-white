using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace KinopoiskWhite.Providers;

using Api;
using Common;
using Api.Models;
using Extensions;

public abstract class Base {
    #pragma warning disable CA1822 // Mark members as static
    public string Name => Constants.ProviderName;
    public string Description => Constants.ProviderDescription;
    #pragma warning restore CA1822 // Mark members as static

    public class Error(string message) : System.Exception(message)
    {
        public class NotImplemented() : Error("Not implemented");
    }
}


public abstract class BaseSingleton: Base {
    protected readonly ILogger _logger;
    public BaseSingleton (ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory?.CreateLogger(GetType());
        _logger?.LogDebug("INIT");
    }
    public ILogger Logger => _logger;
}


public abstract class BaseProvider<TMetadata>(
    ILoggerFactory loggerFactory,
    IHttpClientFactory httpClientFactory,
    IGraphQL graphQL
) : BaseSingleton(loggerFactory)

where TMetadata : BaseMetadata, new()
{
    public new class Error(string message) : Base.Error(message)
    {
        public class EmptySearchString() :
            Base.Error($"Empty search string");
        public class GettingKid(string path) :
            Base.Error($"Get Kinopoisk Id failed [{path ?? "NULL"}]");
        public class GettingRemote(string key) :
            Base.Error($"Getting remote failed by {key}");
    }

    protected readonly IGraphQL _graphql = graphQL;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private static readonly Dictionary<string, object> _cache = [];
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public Task<HttpResponseMessage>
    GetImageResponse(string url, CancellationToken cancellationToken)
    => _httpClientFactory
        .CreateClient(MediaBrowser.Common.Net.NamedClient.Default)
        .GetAsync(url, cancellationToken);

    public async IAsyncEnumerable<TMetadata>
    GetSearchResults(string path, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get search results {path}", path);

        if (string.IsNullOrWhiteSpace(path))
            throw new Error.EmptySearchString();

        var keywords = path.ParseFileName();

        foreach (var (title, year) in keywords)
        {
            if (string.IsNullOrWhiteSpace(title)) continue;

            string keyword = (year == null) ? title : $"{title} {year}";

            await foreach (var film in _graphql.SuggestSearch<TMetadata>(keyword, cancellationToken))
                if (film != null && film.Id != 0)
                    yield return film;
        }
    }

    public async Task<TMetadata>
    GetKinopoiskId(string path, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get kinopoisk ID {path}", path);

        await foreach (var item in GetSearchResults(path, cancellationToken))
            return item;

        throw new Error.GettingKid(path);
    }

    public abstract Task<TMetadata> GetInfoByKid(int kinopoiskId, CancellationToken cancellationToken);
    public async Task<TMetadata> Fetch(int kinopoiskId, CancellationToken cancellationToken)
    => (TMetadata)await WithCache(
        $"fetch_{kinopoiskId}",
        async () => await GetInfoByKid(kinopoiskId, cancellationToken)
    );

    public async Task<object> WithCache(string key, System.Func<Task<object>> task)
    {
        await _semaphore.WaitAsync();

        try
        {
            if (_cache.TryGetValue(key, out object cached))
            {
                _logger.LogDebug("Getting cached by {key}", key);
                return cached;
            }
            _logger.LogDebug("Getting remote by {key}", key);

            var result = await task();

            _cache[key] = result ??
                throw new Error.GettingRemote(key);

            return result;
        }
        catch
        {
            throw;
        }
        finally {
            _semaphore.Release();
        }
    }
}
