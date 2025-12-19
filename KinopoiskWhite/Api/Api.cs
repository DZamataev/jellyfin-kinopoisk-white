using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace KinopoiskWhite.Api;
using Models;
using Extensions;

public interface IApiService
{
    IAsyncEnumerable<FilmInfo> GetSearchResults(string path, CancellationToken cancellationToken);
    Task<FilmInfo> GetKinopoiskId(string path, CancellationToken cancellationToken);
    Task<FilmInfo> Fetch(int kinopoiskId, CancellationToken cancellationToken);
    Task<FilmInfo> FetchByContentId(string contentId, CancellationToken cancellationToken);
    Task<FilmInfo> GetImages(int kid, CancellationToken cancellationToken);
    Task<FilmPerson> GetPerson(int id, CancellationToken cancellationToken);
    IAsyncEnumerable<FilmPerson> GetSearchResultsPerson(string name, CancellationToken cancellationToken);
    Task<FilmPerson> GetKinopoiskIdPerson(string path, CancellationToken cancellationToken);
}

public class ApiService : BaseSingleton, IApiService
{
    private readonly IGraphQL _graphql;

    public ApiService(
        ILogger<ApiService> logger,
        IHttpClientFactory httpClientFactory = null,
        IGraphQL graphQL = null)
    : base(logger, httpClientFactory)
    {
        _graphql = graphQL ?? new GraphQL(null, httpClientFactory);
    }

    public async IAsyncEnumerable<FilmInfo>
    GetSearchResults(string path, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get search results {path}", path);

        var keywords = path.ParseFileName();

        foreach (var (title, year) in keywords)
        {
            if (string.IsNullOrWhiteSpace(title)) continue;

            string keyword = (year == null) ? title : $"{title} {year}";

            await foreach (var film in _graphql.SuggestSearch(keyword, cancellationToken))
                if (film?.Id != null)
                    yield return film;
        }
    }

    public async Task<FilmInfo>
    GetKinopoiskId(string path, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get kinopoisk ID {path}", path);

        await foreach (var film in GetSearchResults(path, cancellationToken))
            return film;

        throw new System.Exception($"Get Kinopoisk Id failed [{path}]");
    }

    public async Task<FilmInfo>
    Fetch(int kinopoiskId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetch {kid}", kinopoiskId);

        var film = await _graphql
            .FilmBaseInfo(kinopoiskId, cancellationToken);
        
        return film ?? throw new System.Exception(
            $"Get Kinopoisk metadata failed KID {kinopoiskId}");
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

    public async Task<FilmInfo>
    GetImages(int kinopoiskId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get images {kid}", kinopoiskId);

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

    public async Task<FilmPerson>
    GetPerson(int id, CancellationToken cancellationToken)
    => await _graphql.GetPerson(id, cancellationToken);

    public async IAsyncEnumerable<FilmPerson>
    GetSearchResultsPerson(string name, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get search results {name}", name);

        if (string.IsNullOrWhiteSpace(name)) yield break;

        await foreach (var person in _graphql.SuggestSearchPerson(name, cancellationToken))
            if (person?.Id != null)
                yield return person;
    }

    public async Task<FilmPerson>
    GetKinopoiskIdPerson(string name, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get kinopoisk ID {name}", name);

        await foreach (var person in GetSearchResultsPerson(name, cancellationToken))
            return person;

        throw new System.Exception($"Get Kinopoisk Id failed [{name}]");
    }
}