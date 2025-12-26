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
using Extensions;
using Interfaces;

public abstract class MovieProvider
(
    ILoggerFactory logger,
    IHttpClientFactory http,
    IGraphQL gql
) : BaseProvider<FilmInfo>(logger, http, gql)
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
    IImageProvider<Movie, FilmInfo>
{
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item) => [
        ImageType.Primary,
        ImageType.Backdrop
    ];

    public async Task<IEnumerable<RemoteImageInfo>>
    GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        if (!item.TryGetDefaultId(out int kid)) return [];

        var result = await WithCache($"images_{kid}", async () =>
        {
            FilmInfo metadata = null;

            if (item.TryGetContentId(out string cid))
            {
                _logger.LogDebug("Fetch by content id {cid}", cid);
                ;
                metadata = await _graphql.FilmPage(cid, cancellationToken)
                    .ConfigureAwait(false);
            } else
            {
                metadata = await _graphql.FilmBaseInfo(kid, cancellationToken)
                    .ConfigureAwait(false);
            }

            var result = await GetImagesById(kid, cancellationToken).ConfigureAwait(false);
            if (metadata != null)
            {
                if (result == null) result = metadata;
                else result = metadata with { Images = result.Images }; 
            }
            return result;
        });

        return result.GetImages();
    }

    protected async Task<FilmInfo>
    GetImagesById(int kinopoiskId, CancellationToken cancellationToken)
    {
        FilmInfo result = null;
        var resultCounter = "";

        foreach (var type in System.Enum.GetValues<FilmImageType>())
        {
            FilmInfo chunk;
            try
            {
                chunk = await _graphql.MovieImagesItems(kinopoiskId, type, cancellationToken);
            }
            catch
            {
                continue;
            }

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