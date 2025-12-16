using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace KinopoiskWhite.Providers;

using Api.Models;
using Extensions;

public class RemoteImageProvider<TItemType>
(
    ILogger<RemoteImageProvider<TItemType>> logger,
    IHttpClientFactory httpClientFactory
) :
    BaseProvider(logger, httpClientFactory),
    IRemoteImageProvider
where TItemType : BaseItem
{
    public bool Supports(BaseItem item) => item is TItemType;

    public IEnumerable<ImageType> GetSupportedImages(BaseItem item) =>
    [
        ImageType.Primary,
        // ImageType.Backdrop,
        // ImageType.Logo,
    ];

    public async Task<IEnumerable<RemoteImageInfo>>
    GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        var kid = item.GetDefaultId();
        // var cid = item.GetContentId();
        // _logger.LogDebug("Loading images by {kid} [{cid}]", kid, cid);
        _logger.LogDebug("Loading images by {kid}", kid);

        if (string.IsNullOrWhiteSpace(kid)) return [];

        FilmInfo meta = null;
        // if (!string.IsNullOrWhiteSpace(cid))
        // {
        //     meta = await _api.FetchByCid(cid, cancellationToken).ConfigureAwait(false);
        //     _logger.LogDebug("Loaded images by {cid}", cid);
        // }
        // if (meta == null)
        // {
        meta = await _api.FetchByKid(kid, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Loaded images by {kid}", kid);
        // }

        return meta.GetImages();
    }
}