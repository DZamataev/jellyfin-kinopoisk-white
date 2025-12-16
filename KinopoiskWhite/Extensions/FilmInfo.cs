using System.Linq;
using System.Collections.Generic;

using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace KinopoiskWhite.Extensions;

using Api.Models;
using Common;

public static class FilmInfoExtensions
{
    public static IEnumerable<RemoteImageInfo> GetImages(this FilmInfo film)
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
            // (ImageType.Backdrop, film.Gallery?.Backdrop),
            // (ImageType.Logo, film.Gallery?.Logo),
        ];

        foreach (var (type, url) in images)
        {
            var result = fill(type, url);

            if (result != null)
                yield return result;
        }
    }
}