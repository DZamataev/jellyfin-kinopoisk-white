using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;

namespace KinopoiskWhite.Providers;
using Api;
using Api.Models;
using Common;
using Extensions;

public abstract class MetadataProvider<TItemType, TLookupInfoType>
(
    ILogger<MetadataProvider<TItemType, TLookupInfoType>> logger,
    IHttpClientFactory httpClientFactory,
    IApiService api
) :
    SearchProvider<TLookupInfoType>(logger, httpClientFactory, api),
    IRemoteMetadataProvider<TItemType, TLookupInfoType>

where TItemType : BaseItem, IHasLookupInfo<TLookupInfoType>
where TLookupInfoType : ItemLookupInfo, new()
{
    protected abstract TItemType GetItem();

    protected abstract Task<BaseMetadata>
    GetKinopoiskId(TLookupInfoType info, CancellationToken cancellationToken);

    protected abstract Task<BaseMetadata>
    Fetch(int kid, CancellationToken cancellationToken);

    public async Task<MetadataResult<TItemType>>
    GetMetadata(TLookupInfoType info, CancellationToken cancellationToken)
    {
        var result = await ResolveInfo(info, cancellationToken);

        var kid = info.GetDefaultId();

        result.FillFrom(await Fetch(kid, cancellationToken));

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

        result.Item = GetItem();

        if (info.HasDefaultId()) return result;

        _logger.LogDebug("KID is empty {item}", info.Name);

        result.QueriedById = false;

        result.FillFrom(await GetKinopoiskId(info, cancellationToken));
        info.SetDefaultId(result.Item.GetDefaultId());

        _logger.LogInformation("Found item {name} as {newName}", info.Name, result.Item.Name);

        return result;
    }
}

public class MovieMetadataProvider
(
    ILogger<MovieMetadataProvider> logger,
    IHttpClientFactory httpClientFactory,
    IApiService api
) :
    MetadataProvider<Movie, MovieInfo>(logger, httpClientFactory, api)
{
    protected override Movie GetItem() => new ();

    protected override async Task<BaseMetadata>
    GetKinopoiskId(MovieInfo info, CancellationToken cancellationToken)
    => await _api.GetKinopoiskId(info.Path, cancellationToken);

    protected override async Task<BaseMetadata>
    Fetch(int kid, CancellationToken cancellationToken)
    => await _api.Fetch(kid, cancellationToken);
}

public class PersonMetadataProvider
(
    ILogger<PersonMetadataProvider> logger,
    IHttpClientFactory httpClientFactory,
    IApiService api
) :
    MetadataProvider<Person, PersonLookupInfo>(logger, httpClientFactory, api)
{
    protected override Person GetItem() => new ();

    protected override async Task<BaseMetadata>
    GetKinopoiskId(PersonLookupInfo info, CancellationToken cancellationToken)
    => await _api.GetKinopoiskIdPerson(info.Name, cancellationToken);

    protected override async Task<BaseMetadata>
    Fetch(int kid, CancellationToken cancellationToken)
    => await _api.GetPerson(kid, cancellationToken);
}