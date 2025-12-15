using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Plugin.Providers;

using Api;
using Common;

public class ImageProvider : IRemoteImageProvider
{
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KinopoiskApi _api;

    public ImageProvider(ILogger<ImageProvider> logger, IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _api = new KinopoiskApi(httpClientFactory);
        _logger = logger;
    }

    public string Name => Constants.ProviderName;
    public static string Description => Constants.ProviderDescription;

    public IEnumerable<ImageType> GetSupportedImages(BaseItem item) => new ImageType[] 
    {
        ImageType.Primary,
        ImageType.Backdrop,
        ImageType.Logo,
    };

    public async Task<IEnumerable<RemoteImageInfo>>
    GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        var kid = item.GetProviderId(Constants.ProviderId);
        var cid = item.GetProviderId(Constants.ProviderName);
        _logger.LogDebug("Loading images by {kid} [{cid}]", kid, cid);

        if (string.IsNullOrWhiteSpace(kid)) return [];

        FilmInfo meta = null;
        if (!string.IsNullOrWhiteSpace(cid))
        {
            meta = await _api.FetchByCid(cid, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Loaded images by {cid}", cid);
        }
        if (meta == null)
        {
            meta = await _api.FetchByKid(kid, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Loaded images by {kid}", kid);
        }

        return FillImages(meta);
    }

    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return _httpClientFactory
            .CreateClient(NamedClient.Default)
            .GetAsync(url, cancellationToken);
    }

    public bool Supports(BaseItem item) => item is Movie;

    private static IEnumerable<RemoteImageInfo> FillImages(FilmInfo film)
    {
        var res = Enumerable.Empty<RemoteImageInfo>();

        static RemoteImageInfo fill(ImageType type, string image)
        {
            if (image == null) return null;

            return new RemoteImageInfo
            {
                Type = type,
                Url = image,
                Language = Constants.ProviderMetadataLanguage,
                ProviderName = Constants.ProviderName,
            };
        }

        (ImageType, string)[] images = [
            (ImageType.Primary, film.Gallery?.Primary),
            (ImageType.Backdrop, film.Gallery?.Backdrop),
            (ImageType.Logo, film.Gallery?.Logo),
        ];

        foreach (var (type, url) in images)
        {
            var result = fill(type, url);

            if (result != null)
                yield return result;
        }
    }
}
