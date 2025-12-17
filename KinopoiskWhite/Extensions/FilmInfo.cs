using System.Linq;
using System.Collections.Generic;

using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace KinopoiskWhite.Extensions;

using Api.Models;
using Common;

using Cache = Dictionary<ImageType, string[]>;

public static class FilmInfoExtensions
{
    public static Cache GetCache(this FilmInfo metadata)
    {
        Cache result = [];
        if (metadata.Gallery == null) return result;

        result[ImageType.Primary] = [ metadata.Gallery?.Primary ];
        result[ImageType.Backdrop] = [ metadata.Gallery?.Backdrop ];
        result[ImageType.Logo] = [ metadata.Gallery?.Logo ];
        return result;
    }

    public static IEnumerable<RemoteImageInfo> GetImages(this Cache cache)
    {
        foreach (var (type, urls) in cache)
        {
            foreach (var url in urls)
            {
                if (url == null) continue;

                yield return new RemoteImageInfo
                {
                    Type = type,
                    Url = url,
                    Language = Constants.ProviderMetadataLanguage,
                    ProviderName = Constants.ProviderName,
                };
            }
        }
    }

    public static IEnumerable<RemoteImageInfo> GetImages(this FilmInfo metadata)
    {
        var cache = metadata.GetCache();
        return cache.GetImages();
    }
}