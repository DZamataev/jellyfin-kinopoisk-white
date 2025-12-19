using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace KinopoiskWhite.Api;
using Models;
using Extensions;

public interface IApiService<T>
{
    IAsyncEnumerable<T> GetSearchResults(string path, CancellationToken cancellationToken);
    Task<T> GetKinopoiskId(string path, CancellationToken cancellationToken);
    Task<T> Fetch(int kinopoiskId, CancellationToken cancellationToken);
    Task<T> GetImages(int kid, CancellationToken cancellationToken);
}

public abstract class ApiService<T>(
    ILogger<ApiService<T>> logger,
    IHttpClientFactory httpClientFactory = null,
    IGraphQL graphQL = null
) :
    BaseSingleton(logger, httpClientFactory),
    IApiService<T>
where T : BaseMetadata
{
    protected readonly IGraphQL _graphql = graphQL ?? new GraphQL(null, httpClientFactory);

    public async IAsyncEnumerable<T>
    GetSearchResults(string path, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get search results {path}", path);

        var keywords = path.ParseFileName();

        foreach (var (title, year) in keywords)
        {
            if (string.IsNullOrWhiteSpace(title)) continue;

            string keyword = (year == null) ? title : $"{title} {year}";

            await foreach (var film in _graphql.SuggestSearch<T>(keyword, cancellationToken))
                if (film?.Id != null)
                    yield return film;
        }
    }

    public async Task<T>
    GetKinopoiskId(string path, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get kinopoisk ID {path}", path);

        await foreach (var item in GetSearchResults(path, cancellationToken))
            return item;

        throw new System.Exception($"Get Kinopoisk Id failed [{path}]");
    }

    protected abstract Task<T> FetchAsync(int kinopoiskId, CancellationToken cancellationToken);
    public async Task<T> Fetch(int kinopoiskId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetch {kid}", kinopoiskId);
        var film = await FetchAsync(kinopoiskId, cancellationToken);
        return film ?? throw new System.Exception(
            $"Get Kinopoisk metadata failed KID {kinopoiskId}");
    }

    protected abstract Task<T> GetImagesAsync(int kinopoiskId, CancellationToken cancellationToken);
    public async Task<T>
    GetImages(int kinopoiskId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get images {kid}", kinopoiskId);
        var result = await GetImagesAsync(kinopoiskId, cancellationToken);
        return result ?? throw new System.Exception(
            $"Get Kinopoisk metadata failed KID {kinopoiskId}");
    }
}

public interface IApiServiceMovie: IApiService<FilmInfo>
{
    Task<FilmInfo> FetchByContentId(string contentId, CancellationToken cancellationToken);
}

public class ApiServiceMovie(
    ILogger<ApiServiceMovie> logger,
    IHttpClientFactory httpClientFactory = null,
    IGraphQL graphQL = null
) :
    ApiService<FilmInfo>(logger, httpClientFactory, graphQL),
    IApiServiceMovie
{
    protected override async Task<FilmInfo> FetchAsync(int kinopoiskId, CancellationToken cancellationToken)
    => await _graphql.FilmBaseInfo(kinopoiskId, cancellationToken);

    protected override async Task<FilmInfo> GetImagesAsync(int kinopoiskId, CancellationToken cancellationToken)
    {
        FilmInfo result = null;
        var resultCounter = "";

        foreach (var type in System.Enum.GetValues<FilmImageType>())
        {
            var chunk = await _graphql.MovieImagesItems(kinopoiskId, type, cancellationToken);

            resultCounter += $"{type}:{chunk.Images?.Items?.Length} ";

            if (result == null) result = chunk;
            else
            {
                FilmImages.ListImage[] images = [.. result?.Images?.Items ?? [],
                                                 .. chunk?.Images?.Items ?? []];

                result = result with
                {
                    Images = new()
                    {
                        Items = images
                    }
                };
            }
        }
        _logger.LogDebug(resultCounter);
        return result;
    }

    public async Task<FilmInfo>
    FetchByContentId(string contentId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetch by content id {cid}", contentId);

        var film = await _graphql
            .FilmPage(contentId, cancellationToken);

        return film ?? throw new System.Exception(
            $"Get Kinopoisk metadata failed CID {contentId}");
    }
}

public interface IApiServicePerson: IApiService<FilmPerson> {}
public class ApiServicePerson (
    ILogger<ApiServicePerson> logger,
    IHttpClientFactory httpClientFactory = null,
    IGraphQL graphQL = null
) :
    ApiService<FilmPerson>(logger, httpClientFactory, graphQL),
    IApiServicePerson
{
    protected override async Task<FilmPerson> FetchAsync(int kinopoiskId, CancellationToken cancellationToken)
    => await _graphql.GetPerson(kinopoiskId, cancellationToken);

    protected override async Task<FilmPerson> GetImagesAsync(int kinopoiskId, CancellationToken cancellationToken)
    => await _graphql.GetPerson(kinopoiskId, cancellationToken);
}
