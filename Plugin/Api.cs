using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.KinopoiskWhite.Api;
using Extensions;

public class KinopoiskApi
{
    private readonly GraphQL _graphql;

    public KinopoiskApi(GraphQL graphql, ILogger<KinopoiskApi> logger, IHttpClientFactory httpClientFactory)
    {
        _graphql = graphql;
    }

    public async Task<string> GetKinopoiskId(ItemLookupInfo info, CancellationToken cancellationToken)
    {
        var keywords = info.Path.ParseFileName();

        foreach (var (title, year) in keywords)
        {
            string keyword = (year == null) ? title : $"{title} {year}";

            var film = await _graphql
                .SuggestSearch(keyword, cancellationToken)
                .ConfigureAwait(false);

            if (film?.Id != null)
                return System.Convert.ToString(film.Id);
        }
        throw new System.Exception($"Get Kinopoisk Id failed [{info.Name}].\n{keywords}");
    }

    public async Task Fetch<T>(MetadataResult<T> itemResult, string kinopoiskId, string language, string country, CancellationToken cancellationToken)
            where T : BaseItem
    {
        if (string.IsNullOrWhiteSpace(kinopoiskId))
        {
            throw new System.ArgumentNullException(nameof(kinopoiskId));
        }

        var kid = System.Convert.ToInt32(kinopoiskId);
        var film = await _graphql
            .FilmBaseInfo(kid, cancellationToken)
            .ConfigureAwait(false);
        
        if (film != null)
        {
            film.Fill(itemResult);
            return;
        }

        throw new System.Exception(
            $"Get Kinopoisk metadata failed KID {kinopoiskId} [{itemResult.Item.Name}].");
    }
}