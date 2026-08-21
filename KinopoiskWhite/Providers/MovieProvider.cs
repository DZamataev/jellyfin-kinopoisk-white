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
    public System.Collections.Generic.IEnumerable<string> GetSearchKeywords(MovieInfo info)
    {
        // Our ParseFileName cleans odd/transliterated/release-junk filenames better
        // than Jellyfin's own parse, so drive the search from the raw path first and
        // keep Jellyfin's cleaned Name only as a fallback.
        if (!string.IsNullOrWhiteSpace(info.Path)) yield return info.Path;
        if (!string.IsNullOrWhiteSpace(info.Name)) yield return info.Name;
    }

    // Prefer the year our own parser finds in the filename; fall back to Jellyfin's
    // year (which can also come from the folder name, which we don't see). The year is
    // only a soft preference for result selection, so a stray value is harmless.
    public int? GetSearchYear(MovieInfo info)
    {
        var parsed = string.IsNullOrWhiteSpace(info.Path)
            ? System.Array.Empty<(string, int?)>()
            : KinopoiskWhite.Extensions.StringExtensions.ParseFileName(info.Path);
        var ourYear = System.Linq.Enumerable.FirstOrDefault(parsed, x => x.Item2 != null).Item2;
        return ourYear ?? info.Year;
    }
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