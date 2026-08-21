using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

namespace KinopoiskWhite.Providers.Interfaces;

using Common;
using Api.Models;
using Extensions;

public interface IImageProvider<TItemType, TMetadata>
: IRemoteImageProvider

where TItemType : BaseItem
where TMetadata : BaseMetadata
{
    bool IImageProvider.Supports(BaseItem item) => item is TItemType;

    public IEnumerable<RemoteImageInfo> Convert((ImageType, string)[] images)
    {
        foreach (var (type, url) in images)
            if (!string.IsNullOrWhiteSpace(url))
                yield return new()
                {
                    Type = type,
                    Url = url,
                    Language = Constants.ProviderMetadataLanguage,
                    ProviderName = Constants.ProviderName,
                };
    }
}

public interface IFilmImageProvider<TItemType, TMetadata>
: IImageProvider<TItemType, TMetadata>

where TItemType : BaseItem
where TMetadata : BaseMetadata
{
    ILogger Logger { get; }
    Task<TMetadata> GetInfoByKid(int kinopoiskId, CancellationToken cancellationToken);
    Task<TMetadata> GetInfoByContentId(string contentId, CancellationToken cancellationToken);
    Task<TMetadata> GetImagesItems(int kinopoiskId, FilmImageType type, CancellationToken cancellationToken);
    Task<object> WithCache(string key, System.Func<Task<object>> task);

    async Task<IEnumerable<RemoteImageInfo>>
    IRemoteImageProvider.GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        if (!item.TryGetDefaultId(out int kid)) return [];

        var images = ((ImageType, string)[])await WithCache($"images_{kid}", async () =>
        {
            TMetadata metadata = null;
            HashSet<(ImageType, string)> images = [];

            metadata = await GetInfoByKid(kid, cancellationToken);
            foreach (var img in metadata?.GetImages() ?? [])
                images.Add(img);

            if (item.TryGetContentId(out string cid))
                try
                {
                metadata = await GetInfoByContentId(cid, cancellationToken);
                foreach (var img in metadata?.GetImages() ?? [])
                    images.Add(img);
                }
                catch (Base.Error ex)
                {
                    Logger.LogError("{message}", ex.Message);
                }

            foreach (var img in await GetImagesById(kid, cancellationToken))
                images.Add(img);

            return images.ToArray();
        });

        return Convert(images);
    }

    public async Task<(ImageType, string)[]>
    GetImagesById(int kinopoiskId, CancellationToken cancellationToken)
    {
        HashSet<(ImageType, string)> images = [];
        var resultCounter = "";

        foreach (var type in System.Enum.GetValues<FilmImageType>())
        {
            TMetadata chunk;
            try
            {
                chunk = await GetImagesItems(kinopoiskId, type, cancellationToken);
            }
            catch
            {
                continue;
            }

            var count = 0;
            foreach (var img in chunk?.GetImages() ?? [])
            {
                images.Add(img);
                count++;
            }
            if (count > 0)
                resultCounter += $"{type}:{count} ";
        }
        Logger.LogDebug(resultCounter);
        return [.. images];
    }
}