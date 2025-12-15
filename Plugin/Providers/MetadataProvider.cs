using System.Linq;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Entities.Movies;

namespace Plugin.Providers;
using Api;
using Common;
using Extensions;

public class KinopoiskItemProvider : IRemoteMetadataProvider<Movie, MovieInfo>
{
    public string Name => Constants.ProviderName;
    public static string Description => Constants.ProviderDescription;

    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KinopoiskApi _api;

    public KinopoiskItemProvider(ILogger<KinopoiskItemProvider> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _api = new KinopoiskApi(httpClientFactory);
    }

    public Task<MetadataResult<Movie>>
    GetMetadata(MovieInfo info, CancellationToken cancellationToken)
    => GetResult<Movie>(info, cancellationToken);

    private async Task<MetadataResult<T>>
    GetResult<T>(ItemLookupInfo info, CancellationToken cancellationToken)
    where T : BaseItem, new()
    {
        var result = new MetadataResult<T>
        {
            Item = new T(),
            QueriedById = true,
            Provider = Constants.ProviderName,
            ResultLanguage = Constants.ProviderMetadataLanguage,
        };

        var kid = info.GetProviderId(Constants.ProviderId);

        if (string.IsNullOrWhiteSpace(kid))
        {
            _logger.LogDebug("KID is empty {item}", info.Name);

            result.QueriedById = false;

            try
            {
                var key = await _api.GetKinopoiskId(info.Path, cancellationToken);

                _logger.LogInformation("Found KID {kid} [{cid}] for {item}",
                                       key.Kid, key.ContentId, info.Name);
                kid = key.Kid;
            }
            catch
            {
                _logger.LogError("KID not found for {item}", info.Name);
            }
        }

        if (string.IsNullOrEmpty(kid)) return result;

        var meta = await _api.FetchByKid(kid, cancellationToken);

        meta.Fill(result);

        _logger.LogInformation("Metadata loaded for {kid}", kid);

        return result;
    }

    public Task<IEnumerable<RemoteSearchResult>>
    GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
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

    public Task<HttpResponseMessage>
    GetImageResponse(string url, CancellationToken cancellationToken)
    => _httpClientFactory
        .CreateClient(MediaBrowser.Common.Net.NamedClient.Default)
        .GetAsync(url, cancellationToken);
}