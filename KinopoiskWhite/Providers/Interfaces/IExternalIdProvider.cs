using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

namespace KinopoiskWhite.Providers.Interfaces;

using Common;

public interface IExternalIdProvider<TItemType>: IExternalId
where TItemType : BaseItem
{
    string BaseUrl => "https://www.kinopoisk.ru";
    string IExternalId.Key => Constants.ProviderId;
    string IExternalId.ProviderName => Constants.ProviderNameShort;
    string IExternalId.UrlFormatString => $"{BaseUrl}/{ExternalIdPath}/{{0}}";
    ExternalIdMediaType? IExternalId.Type => null;
    bool IExternalId.Supports(IHasProviderIds item) => item is TItemType;
    string ExternalIdPath { get; }
}