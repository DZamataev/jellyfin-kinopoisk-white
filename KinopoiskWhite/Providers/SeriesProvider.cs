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
using Extensions;
using Interfaces;

public abstract class SeriesProvider
(
    ILoggerFactory logger,
    IHttpClientFactory http,
    IGraphQL gql
) : BaseProvider<FilmInfo>(logger, http, gql)
{
    protected override async Task<FilmInfo>
    FetchAsync(int tvSeriesId, CancellationToken cancellationToken)
    => await _graphql.CallAndDeserialize(
        "TVSeriesBaseInfo", new
        {
            tvSeriesId,
            isAuthorized = false,
            actorsLimit = 10,
            voiceOverActorsLimit = 0,
            relatedMoviesLimit = 0,
            checkSilentInvoiceAvailability = false,
            withPurchaseOptions = false,
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