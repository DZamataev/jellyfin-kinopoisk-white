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

public class RemoteImageProvider
(
    ILogger<RemoteImageProvider> logger,
    IHttpClientFactory httpClientFactory,
    IApiService api
) :
    BaseProvider(logger, httpClientFactory, api),
    IRemoteImageProvider
{
    public bool Supports(BaseItem item) => (
        item is Movie ||
        item is Person
    );
    private readonly Dictionary<int, FilmInfo> _cache = [];

    public IEnumerable<ImageType> GetSupportedImages(BaseItem item) =>
    [
        ImageType.Primary,
        ImageType.Backdrop,
    ];

    public async Task<IEnumerable<RemoteImageInfo>>
    GetImages(BaseItem item, CancellationToken cancellationToken)
    => item switch
    {
        Movie movie => await GetImages(movie, cancellationToken),
        Person person => await GetImages(person, cancellationToken),
        _ => [],
    };

    public async Task<IEnumerable<RemoteImageInfo>>
    GetImages(Movie item, CancellationToken cancellationToken)
    {
        if (!item.TryGetDefaultId(out int kid)) return [];

        if (_cache.TryGetValue(kid, out FilmInfo result))
            _logger.LogDebug("Getting cached by {kid}", kid);

        else
        {
            if (item.TryGetContentId(out string cid))
                _cache[kid] = await _api.FetchByContentId(cid, cancellationToken)
                    .ConfigureAwait(false);

            result = await _api.GetImages(kid, cancellationToken).ConfigureAwait(false);

            if (_cache.TryGetValue(kid, out FilmInfo _))
                result = _cache[kid] with { Images = result.Images };

            _cache[kid] = result;
        }
        return result.GetImages();
    }

    public async Task<IEnumerable<RemoteImageInfo>>
    GetImages(Person item, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting images for {name}", item.Name);

        if (!item.TryGetDefaultId(out int kid)) return [];

        var result = await _api.GetPerson(kid, cancellationToken).ConfigureAwait(false);
        return result.GetImages();
    }
}