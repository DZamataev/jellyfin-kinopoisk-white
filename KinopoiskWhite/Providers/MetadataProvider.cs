using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Entities.Movies;

namespace KinopoiskWhite.Providers;
using Common;
using Extensions;


public abstract class RemoteMetadataProvider<TItemType, TLookupInfoType>
(
    ILogger<RemoteMetadataProvider<TItemType, TLookupInfoType>> logger,
    IHttpClientFactory httpClientFactory
) :
    SearchProvider<TLookupInfoType>(logger, httpClientFactory),
    IRemoteMetadataProvider<TItemType, TLookupInfoType>

where TItemType : BaseItem, IHasLookupInfo<TLookupInfoType>
where TLookupInfoType : ItemLookupInfo, new()
{
    protected abstract TItemType GetItem();

    public async Task<MetadataResult<TItemType>>
    GetMetadata(TLookupInfoType info, CancellationToken cancellationToken)
    {
        var result = await ResolveInfo(info, cancellationToken);

        result.Item = GetItem();

        result.FillFrom(await _api.FetchByKid(info.GetDefaultId(), cancellationToken));

        _logger.LogInformation("Metadata loaded for {item}", info.Name);

        return result;
    }

    async Task<MetadataResult<TItemType>>
    ResolveInfo(TLookupInfoType info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<TItemType>
        {
            QueriedById = true,
            Provider = Constants.ProviderName,
            ResultLanguage = Constants.ProviderMetadataLanguage,
        };

        if (info.HasDefaultId()) return result;

        _logger.LogDebug("KID is empty {item}", info.Name);

        result.QueriedById = false;

        result.FillFrom(await _api.GetKinopoiskId(info.Path, cancellationToken));

        _logger.LogInformation("Found item {0} as {1}", info.Name, result.Item.Name);

        return result;
    }
}


public class MovieMetadataProvider
(
    ILogger<MovieMetadataProvider> logger,
    IHttpClientFactory httpClientFactory
) :
    RemoteMetadataProvider<Movie, MovieInfo>(logger, httpClientFactory)
{
    protected override Movie GetItem() => new ();
}