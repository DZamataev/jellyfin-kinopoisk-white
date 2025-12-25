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

using Api.Models;
using Extensions;
using Interfaces;


public abstract class MovieProvider(ILoggerFactory logger, IHttpClientFactory http)
: BaseProvider<FilmInfo>(logger, http)
{
    protected override async Task<FilmInfo>
    FetchAsync(int kinopoiskId, CancellationToken cancellationToken)
    => await _graphql.FilmBaseInfo(kinopoiskId, cancellationToken);
}

public class MovieExternalId(ILoggerFactory logger)
: BaseSingleton(logger), IExternalIdProvider<Movie>
{
    public string ExternalIdPath => "film";
}


public class MovieMetadataProvider(ILoggerFactory logger, IHttpClientFactory http)
:
    MovieProvider(logger, http),
    ISearchProvider<MovieInfo, FilmInfo>,
    IMetadataProvider<Movie, MovieInfo, FilmInfo>
{
    public string GetSearchKeyword(MovieInfo info) => info.Path;
}

public class MovieImageProvider(ILoggerFactory logger, IHttpClientFactory http)
: MovieProvider(logger, http), IImageProvider<Movie, FilmInfo>
{
    private readonly Dictionary<int, FilmInfo> _cache = [];
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item) => [
        ImageType.Primary,
        ImageType.Backdrop
    ];

    public async Task<IEnumerable<RemoteImageInfo>>
    GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        if (!item.TryGetDefaultId(out int kid)) return [];

        if (_cache.TryGetValue(kid, out FilmInfo result))
            _logger.LogDebug("Getting cached by {kid}", kid);

        else
        {
            if (item.TryGetContentId(out string cid)) {
                _logger.LogDebug("Fetch by content id {cid}", cid);
;
                _cache[kid] = await _graphql.FilmPage(cid, cancellationToken)
                    .ConfigureAwait(false);
            }

            result = await GetImagesById(kid, cancellationToken).ConfigureAwait(false);

            if (_cache.TryGetValue(kid, out FilmInfo _))
                result = _cache[kid] with { Images = result.Images };

            _cache[kid] = result;
        }
        return result.GetImages();
    }

    protected async Task<FilmInfo>
    GetImagesById(int kinopoiskId, CancellationToken cancellationToken)
    {
        FilmInfo result = null;
        var resultCounter = "";

        foreach (var type in System.Enum.GetValues<FilmImageType>())
        {
            var chunk = await _graphql.MovieImagesItems(kinopoiskId, type, cancellationToken);

            resultCounter += $"{type}:{chunk.Images?.Items?.Length} ";

            if (result == null) result = chunk;
            else
            {
                FilmImages.ListImage[] images = [.. result?.Images?.Items ?? [],
                                                 .. chunk?.Images?.Items ?? []];

                result = result with
                {
                    Images = new()
                    {
                        Items = images
                    }
                };
            }
        }
        _logger.LogDebug(resultCounter);
        return result;
    }
}