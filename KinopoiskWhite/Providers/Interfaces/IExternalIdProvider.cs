using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

namespace KinopoiskWhite.Providers.Interfaces;

using Common;
using KinopoiskWhite.Extensions;

public interface IExternalIdProvider<TItemType>: IExternalUrlProvider
where TItemType : BaseItem
{
    string BaseUrl => "https://www.kinopoisk.ru";
    string IExternalUrlProvider.Name => Constants.ProviderNameShort;
    string ExternalIdPath { get; }
    IEnumerable<string> IExternalUrlProvider.GetExternalUrls(BaseItem item)
    {
        if (item is TItemType && item.TryGetDefaultId(out var kid))
            yield return $"{BaseUrl}/{ExternalIdPath}/{kid}";
    }
}