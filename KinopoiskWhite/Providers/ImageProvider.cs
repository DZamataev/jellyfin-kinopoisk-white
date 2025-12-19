using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace KinopoiskWhite.Providers;

using Api;
using Api.Models;
using Extensions;


public class MovieImageProvider
(
    ILogger<MovieImageProvider> logger,
    IHttpClientFactory httpClientFactory,
    IApiService<FilmInfo> api
) :
    BaseProvider<FilmInfo>(logger, httpClientFactory, api),
    IRemoteImageProvider
{
    private readonly Dictionary<int, FilmInfo> _cache = [];
    public bool Supports(BaseItem item) => item is Movie;
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
            // if (item.TryGetContentId(out string cid))
            //     _cache[kid] = await _api.FetchByContentId(cid, cancellationToken)
            //         .ConfigureAwait(false);

            result = await _api.GetImages(kid, cancellationToken).ConfigureAwait(false);

            if (_cache.TryGetValue(kid, out FilmInfo _))
                result = _cache[kid] with { Images = result.Images };

            _cache[kid] = result;
        }
        return result.GetImages();
    }
}

public class PersonImageProvider
(
    ILogger<PersonImageProvider> logger,
    IHttpClientFactory httpClientFactory,
    IApiService<FilmPerson> api
) :
    BaseProvider<FilmPerson>(logger, httpClientFactory, api),
    IRemoteImageProvider
{
    private readonly Dictionary<int, FilmPerson> _cache = [];
    public bool Supports(BaseItem item) => item is Person;
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item) => [ImageType.Primary];

    public async Task<IEnumerable<RemoteImageInfo>>
    GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        if (!item.TryGetDefaultId(out int kid)) return [];

        _logger.LogDebug("Getting images for {name}", item.Name);

        if (_cache.TryGetValue(kid, out FilmPerson result))
            _logger.LogDebug("Getting cached by {kid}", kid);

        else
        {
            result = await _api.GetImages(kid, cancellationToken).ConfigureAwait(false);
            _cache[kid] = result;
        }
        return result.GetImages();
    }
}