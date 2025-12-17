using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace KinopoiskWhite.Api;
using Providers;
using Models;
using Extensions;
using Microsoft.Extensions.Logging;

public interface IApiService
{
    Task<FilmInfo> GetKinopoiskId(string path, CancellationToken cancellationToken);
    Task<FilmInfo> Fetch(int kinopoiskId, CancellationToken cancellationToken);
    Task<FilmInfo> FetchByContentId(string contentId, CancellationToken cancellationToken);
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
        var film = await _graphql
            .FilmBaseInfo(kinopoiskId, cancellationToken);

        return film ?? throw new System.Exception(
            $"Get Kinopoisk metadata failed KID {kinopoiskId}");
    }

    public async Task<FilmInfo>
    FetchByContentId(string contentId, CancellationToken cancellationToken)
    {
        var film = await _graphql
            .FilmPage(contentId, cancellationToken);

        return film ?? throw new System.Exception(
            $"Get Kinopoisk metadata failed CID {contentId}");
    }

    // public async Task
    // GetImages(string kid, CancellationToken cancellationToken)
    // {
    //     var film = await _graphql
    //         .FilmPage(contentId, cancellationToken);

    //     return film ?? throw new System.Exception(
    //         $"Get Kinopoisk metadata failed CID {contentId}");
    // }
}