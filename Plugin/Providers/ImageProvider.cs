using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
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

    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        var kinopoiskId = item.GetProviderId(Constants.ProviderName);
        if (string.IsNullOrWhiteSpace(kinopoiskId)) return [];

        var metadata = await _api.Fetch(kinopoiskId, cancellationToken).ConfigureAwait(false);
        return FillImages(metadata);
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
