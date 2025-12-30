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
    public override Task<FilmInfo> GetInfoByKid(int kinopoiskId, CancellationToken cancellationToken)
    => _graphql.FilmBaseInfo(kinopoiskId, cancellationToken);
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

    public async Task<IEnumerable<RemoteImageInfo>>
    GetImages(BaseItem item, CancellationToken cancellationToken)
    => await ((IFilmImageProvider<Movie, FilmInfo>)this).GetAllImages(item, cancellationToken);

    public Task<FilmInfo> GetInfoByContentId(string contentId, CancellationToken cancellationToken)
    => _graphql.FilmPage(contentId, cancellationToken);

    public Task<FilmInfo> GetImagesItems(int kinopoiskId, FilmImageType type, CancellationToken cancellationToken)
    => _graphql.MovieImagesItems(kinopoiskId, type, cancellationToken);
}