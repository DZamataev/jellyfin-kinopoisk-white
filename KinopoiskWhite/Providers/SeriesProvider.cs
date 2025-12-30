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
    => _graphql.CallAndDeserialize(
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
    public string ExternalIdPath => "film";
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
    public string GetSearchKeyword(SeriesInfo info) => info.Name;
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

    public async Task<IEnumerable<RemoteImageInfo>>
    GetImages(BaseItem item, CancellationToken cancellationToken)
    => await ((IFilmImageProvider<Series, FilmInfo>)this).GetAllImages(item, cancellationToken);

    public Task<FilmInfo> GetInfoByContentId(string contentId, CancellationToken cancellationToken)
    => null;

    public Task<FilmInfo> GetImagesItems(int kinopoiskId, FilmImageType type, CancellationToken cancellationToken)
    => _graphql.MovieImagesItems(kinopoiskId, type, cancellationToken);
}