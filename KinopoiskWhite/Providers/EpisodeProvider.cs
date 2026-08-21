using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Entities.TV;

namespace KinopoiskWhite.Providers;

using Api;
using Api.Models;
using Interfaces;
using Extensions;
using Common;
using System.Linq;

public class EpisodeMetadataProvider
(
    ILoggerFactory logger,
    IHttpClientFactory http,
    IGraphQL gql
) :
    BaseProvider<FilmEpisode>(logger, http, gql),
    ISearchProvider<EpisodeInfo, FilmEpisode>,
    IMetadataProvider<Episode, EpisodeInfo, FilmEpisode>
{
    public override Task<FilmEpisode> GetInfoByKid(int kinopoiskId, CancellationToken cancellationToken)
    => Task.FromResult<FilmEpisode>(null);

    // Episodes aren't matched by keyword here; yield nothing so the caller reports a
    // graceful "not found" instead of throwing NotImplementedException.
    public System.Collections.Generic.IEnumerable<string> GetSearchKeywords(EpisodeInfo info)
    {
        yield break;
    }

    public async Task<MetadataResult<Episode>> GetMetadata
    (EpisodeInfo info, CancellationToken cancellationToken)
    {
        MetadataResult<Episode> result = new()
        {
            Provider = Constants.ProviderName,
            ResultLanguage = Constants.ProviderMetadataLanguage,
        };

        if (!info.SeriesProviderIds.TryGetValue(Constants.ProviderId, out string skid)) return result;
        if (!int.TryParse(skid, out var kid)) return result;

        var episodes = (FilmEpisode[])await WithCache($"fetch_episodes_{kid}", async () =>
        {
            FilmEpisode[] episodes = [];

            if (info.SeasonProviderIds.TryGetValue(Constants.ProviderName, out string cid))
                try
                {
                    episodes = await _graphql.CallAndDeserialize<FilmEpisode[]>(
                        "SerialStructureSeason", new {
                            contentId = cid,
                            limit = 50,
                            offset = 0,
                            withUserData = false
                        },
                        "data.seasonByContentId.episodes.items", cancellationToken);
                }
                catch (Base.Error ex)
                {
                    Logger.LogError("{message}", ex.Message);
                }

            if (episodes.Length == 0)
                episodes = await _graphql.CallAndDeserialize<FilmEpisode[]>(
                    "TvSeriesEpisodes", new { tvSeriesId = kid, episodesLimit = 30 },
                    "data.tvSeries.releasedEpisodes.items", cancellationToken);

            return episodes;
        });

        var episode = episodes.FirstOrDefault(e =>
            e.Number == info.IndexNumber &&
            e.Season?.Number == info.ParentIndexNumber
        );

        result.Item = new();
        result.FillFrom(episode);

        return result;
    }
}


/*
public class EpisodeImageProvider
(
    ILoggerFactory logger,
    IHttpClientFactory http,
    IGraphQL gql
) :
    EpisodeProvider(logger, http, gql),
    IFilmImageProvider<Episode, FilmInfo>
{
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item) => [
        ImageType.Primary,
        ImageType.Backdrop
    ];

    public Task<FilmInfo> GetInfoByContentId(string contentId, CancellationToken cancellationToken)
    => Task.FromResult<FilmInfo>(null);

    public Task<FilmInfo> GetImagesItems(int id, FilmImageType type, CancellationToken cancellationToken)
    => _graphql.CallAndDeserialize<FilmInfo>(
        "MovieImagesItems", new { id, type, offset = 0, limit = 50 },
        "data.movie", cancellationToken);
}
*/