using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Plugin.Api;
using Models;
using Extensions;

public class KinopoiskApi
{
    private readonly GraphQL _graphql;

    public KinopoiskApi(IHttpClientFactory httpClientFactory = null, GraphQL graphQL = null)
    {
        _graphql = graphQL ?? new GraphQL(httpClientFactory);
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
    FetchByKid(string kinopoiskId, CancellationToken cancellationToken)
    {
        var film = await _graphql
            .FilmBaseInfo(System.Convert.ToInt32(kinopoiskId), cancellationToken);

        return film ?? throw new System.Exception(
            $"Get Kinopoisk metadata failed KID {kinopoiskId}");
    }

    public async Task<FilmInfo>
    FetchByCid(string contentId, CancellationToken cancellationToken)
    {
        var film = await _graphql
            .FilmPage(contentId, cancellationToken);

        return film ?? throw new System.Exception(
            $"Get Kinopoisk metadata failed CID {contentId}");
    }
}