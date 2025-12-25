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
}


public abstract class BaseSingleton: Base {
    protected readonly ILogger _logger;
    public BaseSingleton (ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(GetType());
        _logger?.LogDebug("INIT");
    }
    public ILogger Logger => _logger;
}


public abstract class BaseProvider<TMetadata>(
    ILoggerFactory loggerFactory,
    IHttpClientFactory httpClientFactory
) : BaseSingleton(loggerFactory)

where TMetadata : BaseMetadata
{
    private static IGraphQL __graphql = null;
    protected readonly IGraphQL _graphql = __graphql ??= new GraphQL(loggerFactory, httpClientFactory);

    protected readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public Task<HttpResponseMessage>
    GetImageResponse(string url, CancellationToken cancellationToken)
    => _httpClientFactory
        .CreateClient(MediaBrowser.Common.Net.NamedClient.Default)
        .GetAsync(url, cancellationToken);

    public async IAsyncEnumerable<TMetadata>
    GetSearchResults(string path, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get search results {path}", path);

        var keywords = path.ParseFileName();

        foreach (var (title, year) in keywords)
        {
            if (string.IsNullOrWhiteSpace(title)) continue;

            string keyword = (year == null) ? title : $"{title} {year}";

            await foreach (var film in _graphql.SuggestSearch<TMetadata>(keyword, cancellationToken))
                if (film?.Id != null)
                    yield return film;
        }
    }

    public async Task<TMetadata>
    GetKinopoiskId(string path, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get kinopoisk ID {path}", path);

        await foreach (var item in GetSearchResults(path, cancellationToken))
            return item;

        throw new System.Exception($"Get Kinopoisk Id failed [{path}]");
    }

    protected abstract Task<TMetadata> FetchAsync(int kinopoiskId, CancellationToken cancellationToken);
    public async Task<TMetadata> Fetch(int kinopoiskId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetch {kid}", kinopoiskId);
        var film = await FetchAsync(kinopoiskId, cancellationToken);
        return film ?? throw new System.Exception(
            $"Get Kinopoisk metadata failed KID {kinopoiskId}");
    }
}
