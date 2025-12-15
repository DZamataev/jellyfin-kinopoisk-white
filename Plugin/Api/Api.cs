using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Plugin.Api;
using Common;

public class KinopoiskApi
{
    private readonly GraphQL _graphql;

    public KinopoiskApi(IHttpClientFactory httpClientFactory)
    {
        _graphql = new GraphQL(httpClientFactory);
    }

    public async Task<FilmInfo> GetKinopoiskId(string path, CancellationToken cancellationToken)
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

            if (film?.Id != null && film.ContentId != null)
                return film;
        }
        throw new System.Exception($"Get Kinopoisk Id failed [{path}].\n{keywords}");
    }

    public async Task<FilmInfo> Fetch(string contentId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(contentId))
        {
            throw new System.ArgumentNullException(nameof(contentId));
        }

        var film = await _graphql
            .FilmPage(contentId, cancellationToken)
            .ConfigureAwait(false);

        return film ?? throw new System.Exception(
            $"Get Kinopoisk metadata failed KID {contentId}");
    }
}