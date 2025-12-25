using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

namespace KinopoiskWhite.Providers.Interfaces;
using Api.Models;

public interface IImageProvider<TItemType>
: IRemoteImageProvider

where TItemType : BaseItem
{
    bool IImageProvider.Supports(BaseItem item) => item is TItemType;
}