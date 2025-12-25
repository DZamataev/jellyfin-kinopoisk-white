using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using MediaBrowser.Model.Providers;
using MediaBrowser.Controller.Providers;

namespace KinopoiskWhite.Providers.Interfaces;
using Api.Models;
using Extensions;

public interface ISearchProvider<TLookupInfoType, TMetadata>
: IRemoteSearchProvider<TLookupInfoType>

where TLookupInfoType : ItemLookupInfo
where TMetadata : BaseMetadata
{
    async Task<IEnumerable<RemoteSearchResult>>
    IRemoteSearchProvider<TLookupInfoType>.GetSearchResults
    (TLookupInfoType searchInfo, CancellationToken cancellationToken)
    {
        var results = new List<RemoteSearchResult>();

        if (searchInfo.TryGetDefaultId(out var kid))
            results = [
                (await Fetch(kid, cancellationToken)).GetSearchResult()
            ];


        if (!string.IsNullOrWhiteSpace(searchInfo.Name))
        {
            string keyword = (searchInfo.Year == null)
                ? searchInfo.Name
                : $"{searchInfo.Name} {searchInfo.Year}";

            await foreach (var metadata in GetSearchResults(keyword, cancellationToken))
                results.Add(metadata.GetSearchResult());
        }

        return results;
    }

    Task<TMetadata> Fetch(int kinopoiskId, CancellationToken cancellationToken);
    IAsyncEnumerable<TMetadata> GetSearchResults(string path, CancellationToken cancellationToken);
}