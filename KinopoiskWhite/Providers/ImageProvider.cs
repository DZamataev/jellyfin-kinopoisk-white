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
using Extensions;
using KinopoiskWhite.Api.Models;

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
    private readonly Dictionary<int, FilmInfo> _cache = [];

    public IEnumerable<ImageType> GetSupportedImages(BaseItem item) =>
    [
        ImageType.Primary,
        ImageType.Box,
        ImageType.BoxRear,
        ImageType.Backdrop,
        ImageType.Screenshot,
        ImageType.Art,
        ImageType.Banner,
    ];

    public async Task<FilmInfo>
    GetMetadata(BaseItem item, CancellationToken cancellationToken)
    {
        if (!item.TryGetDefaultId(out int kid))
        {
            _logger.LogDebug("Looking for kid by path {path}", item.Path);

            var initial = await _api.GetKinopoiskId(item.Path, cancellationToken)
                .ConfigureAwait(false);

            kid = initial.Id;

            if (_cache.TryGetValue(kid, out FilmInfo _))
                throw new System.Exception($"Cache conflict {kid}");

            _cache[kid] = initial;
        }

        if (_cache.TryGetValue(kid, out FilmInfo cached))
            _logger.LogDebug("Getting cached by {kid}", kid);

        else
        {
            if (item.TryGetContentId(out string cid))
                _cache[kid] = await _api.FetchByContentId(cid, cancellationToken)
                    .ConfigureAwait(false);

            cached = await _api.GetImages(kid, cancellationToken).ConfigureAwait(false);

            if (_cache.TryGetValue(kid, out FilmInfo _))
                cached = _cache[kid] with { Images = cached.Images };

            _cache[kid] = cached;
        }
        return cached;
    }

    public async Task<IEnumerable<RemoteImageInfo>>
    GetImages(BaseItem item, CancellationToken cancellationToken)
    => (await GetMetadata(item, cancellationToken)).GetImages();
}