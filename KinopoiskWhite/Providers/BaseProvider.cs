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
    protected readonly IHttpClientFactory _httpClientFactory;

    protected BaseSingleton (
        ILogger logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;

        _logger?.LogDebug("INIT");
    }

    public ILogger Logger => _logger;
}


public abstract class BaseProvider<TMetadata>
(
    ILogger logger,
    IHttpClientFactory httpClientFactory,
    IGraphQL graphQL
) : BaseSingleton(logger, httpClientFactory)

where TMetadata : BaseMetadata
{
    protected readonly IGraphQL _graphql = graphQL ?? new GraphQL(null, httpClientFactory);

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

    protected abstract Task<TMetadata> GetImagesAsync(int kinopoiskId, CancellationToken cancellationToken);
    public async Task<TMetadata>
    GetImages(int kinopoiskId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get images {kid}", kinopoiskId);
        var result = await GetImagesAsync(kinopoiskId, cancellationToken);
        return result ?? throw new System.Exception(
            $"Get Kinopoisk metadata failed KID {kinopoiskId}");
    }
}
