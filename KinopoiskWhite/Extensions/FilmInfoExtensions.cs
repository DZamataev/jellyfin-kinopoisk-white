/*
using System.Collections.Generic;

using MediaBrowser.Model.Providers;
using ImageType = MediaBrowser.Model.Entities.ImageType;

namespace KinopoiskWhite.Extensions;

using System.Linq;
using Api.Models;
using Common;

public static class FilmInfoExtensions
{
    private static readonly Dictionary<FilmImageType, ImageType> Mapper = new() {
        {FilmImageType.POSTER, ImageType.Primary},
        {FilmImageType.COVER, ImageType.Primary},
        {FilmImageType.FAN_ART, ImageType.Primary},
        {FilmImageType.WALLPAPER, ImageType.Backdrop},
        {FilmImageType.STILL, ImageType.Backdrop},
        {FilmImageType.SCREENSHOT, ImageType.Backdrop},
    };

    public static IEnumerable<RemoteImageInfo> GetImages1(this FilmInfo metadata)
    {
        Dictionary<ImageType, List<string>> result = [];

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
                if (!Mapper.TryGetValue(item.Type, out ImageType type)) continue;

                if (!result.TryGetValue(type, out var _))
                    result[type] = [];

                result[type].Add(item.Image.Url);
            }
        }

        foreach (var (type, urls) in result)
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

    public static IEnumerable<RemoteImageInfo> GetImages1(this FilmPerson person)
    {
        string[] urls = [
            person.Img?.PosterMedium?.X2,
            .. person.Gallery.Select(item => $"{item.BaseUrl}/576x").ToArray(),
        ];

        foreach (var url in urls.Where(x => x != null))
            yield return new RemoteImageInfo
            {
                Type = ImageType.Primary,
                Url = $"https:{url}",
                Language = Constants.ProviderMetadataLanguage,
                ProviderName = Constants.ProviderName,
            };
    }

}
*/