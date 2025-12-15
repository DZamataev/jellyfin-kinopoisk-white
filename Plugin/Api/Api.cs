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

    public async Task<string> GetKinopoiskId(string path, CancellationToken cancellationToken)
    {
        var keywords = path.ParseFileName();

        foreach (var (title, year) in keywords)
        {
            string keyword = (year == null) ? title : $"{title} {year}";

            var film = await _graphql
                .SuggestSearch(keyword, cancellationToken)
                .ConfigureAwait(false);

            if (film?.Id != null)
                return System.Convert.ToString(film.Id);
        }
        throw new System.Exception($"Get Kinopoisk Id failed [{path}].\n{keywords}");
    }

    public async Task<FilmInfo> Fetch(string kinopoiskId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(kinopoiskId))
        {
            throw new System.ArgumentNullException(nameof(kinopoiskId));
        }

        var kid = System.Convert.ToInt32(kinopoiskId);
        var film = await _graphql
            .FilmBaseInfo(kid, cancellationToken)
            .ConfigureAwait(false);

        return film ?? throw new System.Exception(
            $"Get Kinopoisk metadata failed KID {kinopoiskId}");
    }
}