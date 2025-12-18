using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace KinopoiskWhite.Api;
using Models;
using Extensions;

public interface IApiService
{
    Task<FilmInfo> GetKinopoiskId(string path, CancellationToken cancellationToken);
    Task<FilmInfo> Fetch(int kinopoiskId, CancellationToken cancellationToken);
    Task<FilmInfo> FetchByContentId(string contentId, CancellationToken cancellationToken);
    Task<FilmInfo> GetImages(int kid, CancellationToken cancellationToken);
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

    public async Task<FilmInfo>
    GetKinopoiskId(string path, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get kinopoisk ID {path}", path);

        var keywords = path.ParseFileName();
        FilmInfo film = null;

        foreach (var (title, year) in keywords)
        {
            string keyword = (year == null) ? title : $"{title} {year}";

            try
            {
                film = await _graphql
                    .SuggestSearch(keyword, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                continue;
            }

            if (film?.Id != null)
                return film;
        }
        throw new System.Exception($"Get Kinopoisk Id failed [{path}].\n{keywords}");
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
}