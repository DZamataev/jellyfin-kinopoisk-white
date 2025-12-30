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

    public string GetSearchKeyword(EpisodeInfo info) => throw new System.NotImplementedException();

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
            await _graphql.CallAndDeserialize<FilmEpisode[]>(
                "TvSeriesEpisodes", new { tvSeriesId = kid, episodesLimit = 30 },
                "data.tvSeries.releasedEpisodes.items", cancellationToken)
        );

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