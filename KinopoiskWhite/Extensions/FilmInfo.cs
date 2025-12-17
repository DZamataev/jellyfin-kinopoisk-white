using System.Linq;
using System.Collections.Generic;

using MediaBrowser.Model.Providers;
using ImageType = MediaBrowser.Model.Entities.ImageType;

namespace KinopoiskWhite.Extensions;

using Api.Models;
using Common;
using Cache = Dictionary<ImageType, List<string>>;

public static class FilmInfoExtensions
{
    private static Dictionary<FilmImageType, ImageType> Mapper = new() {
        {FilmImageType.POSTER, ImageType.Primary},
        {FilmImageType.COVER, ImageType.Box},
        {FilmImageType.STILL, ImageType.BoxRear},
        {FilmImageType.WALLPAPER, ImageType.Backdrop},
        {FilmImageType.SCREENSHOT, ImageType.Screenshot},
        {FilmImageType.SHOOTING, ImageType.Backdrop},
        {FilmImageType.FAN_ART, ImageType.Art},
        {FilmImageType.PROMO, ImageType.Banner},
        {FilmImageType.CONCEPT, ImageType.Box},
    };

    private static TValue
    GetOrAddDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
    {
        if (dict.TryGetValue(key, out var value))
            return value;
        
        dict[key] = defaultValue;
        return defaultValue;
    }

    public static Cache GetCache(this FilmInfo metadata)
    {
        Cache result = [];
        var gallery = metadata?.Gallery;
        if (gallery != null)
        {
            result[ImageType.Primary] = [ gallery?.Primary ];
            result[ImageType.Backdrop] = [ gallery?.Backdrop ];
            result[ImageType.Logo] = [ gallery?.Logo ];
        }
        var items = metadata?.Images?.Items;
        if (items != null)
        {
            foreach (var item in items)
            {
                if (!FilmImageType.TryParse(item.Type, false, out FilmImageType ftype)) continue;
                if (!Mapper.TryGetValue(ftype, out ImageType otype)) continue;

                if (!result.TryGetValue(otype, out var values))
                    result[otype] = [];

                result[otype].Add(item.Image.Url);
            }
        }
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