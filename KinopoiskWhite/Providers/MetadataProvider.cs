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

public abstract class MetadataProvider<TItemType, TLookupInfoType, TMetadata>
(
    ILogger<MetadataProvider<TItemType, TLookupInfoType, TMetadata>> logger,
    IHttpClientFactory httpClientFactory,
    IApiService<TMetadata> api
) :
    SearchProvider<TLookupInfoType, TMetadata>(logger, httpClientFactory, api),
    IRemoteMetadataProvider<TItemType, TLookupInfoType>

where TItemType : BaseItem, IHasLookupInfo<TLookupInfoType>, new()
where TLookupInfoType : ItemLookupInfo, new()
where TMetadata : BaseMetadata
{
    protected abstract string GetSearchKeyword(TLookupInfoType info);

    public async Task<MetadataResult<TItemType>>
    GetMetadata(TLookupInfoType info, CancellationToken cancellationToken)
    {
        var result = await ResolveInfo(info, cancellationToken);

        var kid = info.GetDefaultId();

        result.FillFrom(await _api.Fetch(kid, cancellationToken));

        _logger.LogInformation("Metadata loaded for {item}", info.Name);

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

        _logger.LogDebug("KID is empty {item}", info.Name);

        result.QueriedById = false;

        var keyword = GetSearchKeyword(info);
        result.FillFrom(await _api.GetKinopoiskId(keyword, cancellationToken));
        info.SetDefaultId(result.Item.GetDefaultId());

        _logger.LogInformation("Found item {name} as {newName}", info.Name, result.Item.Name);

        return result;
    }
}

public class MovieMetadataProvider
(
    ILogger<MovieMetadataProvider> logger,
    IHttpClientFactory httpClientFactory,
    IApiService<FilmInfo> api
) :
    MetadataProvider<Movie, MovieInfo, FilmInfo>(logger, httpClientFactory, api)
{
    protected override string GetSearchKeyword(MovieInfo info) => info.Path;
}

public class PersonMetadataProvider
(
    ILogger<PersonMetadataProvider> logger,
    IHttpClientFactory httpClientFactory,
    IApiService<FilmPerson> api
) :
    MetadataProvider<Person, PersonLookupInfo, FilmPerson>(logger, httpClientFactory, api)
{
    protected override string GetSearchKeyword(PersonLookupInfo info) => info.Name;
}