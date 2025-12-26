using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

namespace KinopoiskWhite.Providers.Interfaces;
using Api.Models;
using Common;
using Extensions;

public interface IMetadataProvider<TItemType, TLookupInfoType, TMetadata>
: IRemoteMetadataProvider<TItemType, TLookupInfoType>

where TItemType : BaseItem, IHasLookupInfo<TLookupInfoType>, new()
where TLookupInfoType : ItemLookupInfo, new()
where TMetadata : BaseMetadata
{
    ILogger Logger { get; }

    async Task<MetadataResult<TItemType>>
    IRemoteMetadataProvider<TItemType, TLookupInfoType>.GetMetadata
    (TLookupInfoType info, CancellationToken cancellationToken)
    {
        MetadataResult<TItemType> result;
        try
        {
            result = await ResolveInfo(info, cancellationToken);

            var kid = info.GetDefaultId();

            var metadata = await Fetch(kid, cancellationToken);

            result.FillFrom(metadata ?? throw new Base.Error("Metadata is NULL"));
        }
        catch (Base.Error ex)
        {
            Logger.LogError("{message}", ex.Message);
            return null;
        }
        return result;
    }

    async Task<MetadataResult<TItemType>>
    ResolveInfo(TLookupInfoType info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<TItemType>
        {
            Item = new(),
            QueriedById = true,
            Provider = Constants.ProviderName,
            ResultLanguage = Constants.ProviderMetadataLanguage,
        };

        if (info.HasDefaultId()) return result;

        Logger.LogDebug("KID is empty {item}", info.Name);

        result.QueriedById = false;

        var keyword = GetSearchKeyword(info);
        result.FillFrom(await GetKinopoiskId(keyword, cancellationToken));
        info.SetDefaultId(result.Item.GetDefaultId());

        Logger.LogInformation("Found item {name} as {newName}", info.Name, result.Item.Name);

        return result;
    }

    string GetSearchKeyword(TLookupInfoType info);
    Task<TMetadata> Fetch(int kinopoiskId, CancellationToken cancellationToken);
    Task<TMetadata> GetKinopoiskId(string path, CancellationToken cancellationToken);
}
