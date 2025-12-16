using System.Linq;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using MediaBrowser.Model.Providers;
using MediaBrowser.Controller.Providers;

namespace KinopoiskWhite.Providers;

public abstract class SearchProvider<TLookupInfoType>(
    ILogger<SearchProvider<TLookupInfoType>> logger,
    IHttpClientFactory httpClientFactory
) :
    BaseProvider(logger, httpClientFactory),
    IRemoteSearchProvider<TLookupInfoType>
where TLookupInfoType : ItemLookupInfo, new()
{
    public Task<IEnumerable<RemoteSearchResult>>
    GetSearchResults(TLookupInfoType searchInfo, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetSearchResults");

        if (string.IsNullOrEmpty(searchInfo.Name))
        {
            _logger.LogError("GetSearchResults EMPTY");
            return Task.FromResult(Enumerable.Empty<RemoteSearchResult>());
        }

        var results = new List<RemoteSearchResult> {
            new() {
                Name = searchInfo.Name,
                ProductionYear = searchInfo.Year ?? 2033,
                ProviderIds = new Dictionary<string, string> { { "TestProvider", $"test-{searchInfo.Name}" } }
            }
        };

        return Task.FromResult<IEnumerable<RemoteSearchResult>>(results);
    }
}