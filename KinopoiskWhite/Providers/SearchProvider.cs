using System.Net.Http;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using MediaBrowser.Model.Providers;
using MediaBrowser.Controller.Providers;

namespace KinopoiskWhite.Providers;
using Api;
using Api.Models;
using Extensions;

public abstract class SearchProvider<TLookupInfoType, TMetadata>(
    ILogger<SearchProvider<TLookupInfoType, TMetadata>> logger,
    IHttpClientFactory httpClientFactory,
    IApiService<TMetadata> api
) :
    BaseProvider<TMetadata>(logger, httpClientFactory, api),
    IRemoteSearchProvider<TLookupInfoType>
where TLookupInfoType : ItemLookupInfo, new()
where TMetadata : BaseMetadata
{
    public async Task<IEnumerable<RemoteSearchResult>>
    GetSearchResults(TLookupInfoType searchInfo, CancellationToken cancellationToken)
    {
        var results = new List<RemoteSearchResult>();

        if (searchInfo.TryGetDefaultId(out var kid))
            results = [
                (await _api.Fetch(kid, cancellationToken)).GetSearchResult()
            ];
        

        if (!string.IsNullOrWhiteSpace(searchInfo.Name)) {
            string keyword = (searchInfo.Year == null)
                ? searchInfo.Name
                : $"{searchInfo.Name} {searchInfo.Year}";

            await foreach (var metadata in _api.GetSearchResults(keyword, cancellationToken))
                results.Add(metadata.GetSearchResult());
        }

        return results;
    }
}