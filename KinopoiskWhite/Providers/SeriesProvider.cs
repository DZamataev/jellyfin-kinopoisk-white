using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Entities.TV;

namespace KinopoiskWhite.Providers;

using Api;
using Api.Models;
using Interfaces;

public abstract class SeriesProvider
(
    ILoggerFactory logger,
    IHttpClientFactory http,
    IGraphQL gql
) : BaseProvider<FilmInfo>(logger, http, gql)
{
    public override Task<FilmInfo> GetInfoByKid(int tvSeriesId, CancellationToken cancellationToken)
    => _graphql.CallAndDeserialize<FilmInfo>(
        "TvSeriesBaseInfo", new
        {
            tvSeriesId,
            isAuthorized = false,
            checkSilentInvoiceAvailability = false,
            withPurchaseOptions = false,
            actorsLimit = 10,
            voiceOverActorsLimit = 0,
            watchabilityLimit = 0,
            socialArgumentLimit = 0,
        },
        "data.tvSeries", cancellationToken);
}

public class SeriesExternalId(ILoggerFactory logger)
: BaseSingleton(logger), IExternalIdProvider<Series>
{
    public string ExternalIdPath => "series";
}

public class SeriesMetadataProvider
(
    ILoggerFactory logger,
    IHttpClientFactory http,
    IGraphQL gql
) :
    SeriesProvider(logger, http, gql),
    ISearchProvider<SeriesInfo, FilmInfo>,
    IMetadataProvider<Series, SeriesInfo, FilmInfo>
{
    public System.Collections.Generic.IEnumerable<string> GetSearchKeywords(SeriesInfo info)
    {
        if (!string.IsNullOrWhiteSpace(info.Name)) yield return info.Name;
        if (!string.IsNullOrWhiteSpace(info.Path)) yield return info.Path;
    }
}


public class SeriesImageProvider
(
    ILoggerFactory logger,
    IHttpClientFactory http,
    IGraphQL gql
) :
    SeriesProvider(logger, http, gql),
    IFilmImageProvider<Series, FilmInfo>
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