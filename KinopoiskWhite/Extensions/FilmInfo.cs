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
        {FilmImageType.COVER, ImageType.Primary},
        {FilmImageType.FAN_ART, ImageType.Primary},
        {FilmImageType.WALLPAPER, ImageType.Backdrop},
        {FilmImageType.STILL, ImageType.Backdrop},
        {FilmImageType.SCREENSHOT, ImageType.Backdrop},
    };

    public static Cache GetCache(this FilmInfo metadata)
    {
        Cache result = [];

        var gallery = metadata?.Gallery;
        if (gallery != null)
        {
            result[ImageType.Primary] = [
                (gallery.Posters?.Vertical ?? gallery.Posters?.MarketingVertical)?.Url
            ];
            result[ImageType.Logo] = [
                gallery?.Logos?.Horizontal?.Url
            ];
        }

        var items = metadata?.Images?.Items;
        if (items != null)
        {
            foreach (var item in items)
            {
                if (!Mapper.TryGetValue(item.Type, out ImageType otype)) continue;

                if (!result.TryGetValue(otype, out var _))
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

    public static RemoteSearchResult GetSearchResult(this FilmInfo metadata)
    {
        string title;
        if (!string.IsNullOrWhiteSpace(metadata.Title.Russian))
        {
            title = metadata.Title.Russian;
            if (!string.IsNullOrWhiteSpace(metadata.Title.Original))
                title += $" ({metadata.Title.Original})";
        }
        else
            title = metadata.Title.Original;

        RemoteSearchResult result = new()
        {
            Name = title,
            ProductionYear = metadata.ProductionYear,
            ImageUrl = (
                metadata.Gallery?.Posters?.HdVertical ??
                metadata.Gallery?.Posters?.KpVertical ??
                metadata.Gallery?.Posters?.Vertical ??
                metadata.Gallery?.Posters?.MarketingVertical
            )?.Medium,
        };
        result.SetDefaultId(metadata.Id);
        result.SetContentId(metadata.ContentId);
        return result;
    }
}