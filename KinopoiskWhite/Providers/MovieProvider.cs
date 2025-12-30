using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Entities.Movies;

namespace KinopoiskWhite.Providers;

using Api;
using Api.Models;
using Interfaces;

public abstract class MovieProvider
(
    ILoggerFactory logger,
    IHttpClientFactory http,
    IGraphQL gql
) : BaseProvider<FilmInfo>(logger, http, gql)
{
    public override Task<FilmInfo> GetInfoByKid(int filmId, CancellationToken cancellationToken)
    => _graphql.CallAndDeserialize<FilmInfo>(
        "FilmBaseInfo", new
        {
            filmId,
            isAuthorized = false,
            actorsLimit = 10,
            voiceOverActorsLimit = 0,
            relatedMoviesLimit = 0,
            checkSilentInvoiceAvailability = false,
            withPurchaseOptions = false,
            watchabilityLimit = 0,
            socialArgumentLimit = 0,
        },
        "data.film", cancellationToken);

}

public class MovieExternalId(ILoggerFactory logger)
: BaseSingleton(logger), IExternalIdProvider<Movie>
{
    public string ExternalIdPath => "film";
}


public class MovieMetadataProvider
(
    ILoggerFactory logger,
    IHttpClientFactory http,
    IGraphQL gql
) :
    MovieProvider(logger, http, gql),
    ISearchProvider<MovieInfo, FilmInfo>,
    IMetadataProvider<Movie, MovieInfo, FilmInfo>
{
    public string GetSearchKeyword(MovieInfo info) => info.Path;
}

public class MovieImageProvider
(
    ILoggerFactory logger,
    IHttpClientFactory http,
    IGraphQL gql
) :
    MovieProvider(logger, http, gql),
    IFilmImageProvider<Movie, FilmInfo>
{
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item) => [
        ImageType.Primary,
        ImageType.Backdrop
    ];

    public Task<FilmInfo> GetInfoByContentId(string contentUuid, CancellationToken cancellationToken)
    => _graphql.CallAndDeserialize<FilmInfo>(
        "FilmPage",
        new { contentUuid, seasonNumber = 0, episodeNumber = 0, isAuthorized = false },
        "data.movieByContentUuid", cancellationToken);

    public Task<FilmInfo> GetImagesItems(int id, FilmImageType type, CancellationToken cancellationToken)
    => _graphql.CallAndDeserialize<FilmInfo>(
        "MovieImagesItems", new { id, type, offset = 0, limit = 50 },
        "data.movie", cancellationToken);
}