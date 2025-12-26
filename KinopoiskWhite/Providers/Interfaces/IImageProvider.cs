using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

namespace KinopoiskWhite.Providers.Interfaces;

using Api.Models;

public interface IImageProvider<TItemType, TMetadata>
: IRemoteImageProvider

where TItemType : BaseItem
where TMetadata : BaseMetadata
{
    bool IImageProvider.Supports(BaseItem item) => item is TItemType;
}