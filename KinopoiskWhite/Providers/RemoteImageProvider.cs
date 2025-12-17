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
using Cache = Dictionary<ImageType, string[]>;

public class RemoteImageProvider
(
    ILogger<RemoteImageProvider> logger,
    IHttpClientFactory httpClientFactory,
    IApiService api
) :
    BaseProvider(logger, httpClientFactory, api),
    IRemoteImageProvider
{
    public bool Supports(BaseItem item) => item is Movie;
    private Dictionary<string, Cache> _cache = [];

    public IEnumerable<ImageType> GetSupportedImages(BaseItem item) =>
    [
        ImageType.Primary,
        // ImageType.Backdrop,
        // ImageType.Logo,
    ];

    public async Task<IEnumerable<RemoteImageInfo>>
    GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        if (!item.TryGetDefaultId(out string kid)) return [];

        if (_cache.TryGetValue(kid, out Cache cache))
            _logger.LogDebug("Getting cached images by {kid}", kid);
        else
        {
            _logger.LogDebug("Loading images by {kid}", kid);
            var meta = await _api.FetchByKid(kid, cancellationToken).ConfigureAwait(false);
            cache = meta.GetCache();
            _cache[kid] = cache;
        }

        var result = cache.GetImages();

        foreach (var img in result)
            _logger.LogDebug("{type} {img}", img.Type, img.Url);
            
        return result;
    }
}