using System.Linq;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using System.Text.Json;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Configuration;

namespace Jellyfin.Plugin.KinopoiskWhite;

public class KinopoiskItemProvider : IRemoteMetadataProvider<Movie, MovieInfo>
{
    public string Name => Constants.ProviderName;
    public static string Description => Constants.ProviderDescription;

    // private readonly IHttpClientFactory _httpClientFactory;
    // private readonly ILibraryManager _libraryManager;
    private readonly ILogger _logger;
    private readonly KinopoiskApi _api;

    public KinopoiskItemProvider(
        // IHttpClientFactory httpClientFactory,
        // ILibraryManager libraryManager,
        ILogger<KinopoiskItemProvider> logger,
        KinopoiskApi api
        )
    {
        // _httpClientFactory = httpClientFactory;
        // _libraryManager = libraryManager;
        _logger = logger;
        _api = api;
    }

    public Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
    {
        return GetResult<Movie>(info, cancellationToken);
    }

    private async Task<MetadataResult<T>> GetResult<T>(ItemLookupInfo info, CancellationToken cancellationToken)
    where T : BaseItem, new()
    {
        var result = new MetadataResult<T>
        {
            Item = new T(),
            QueriedById = true,
            Provider = Constants.ProviderName,
            ResultLanguage = Constants.ProviderMetadataLanguage,
        };

        var kinopoiskId = info.GetProviderId(Constants.ProviderName);
        if (string.IsNullOrWhiteSpace(kinopoiskId))
        {
            kinopoiskId = await _api.GetKinopoiskId(info, cancellationToken)
                .ConfigureAwait(false);
            result.QueriedById = false;
        }

        if (!string.IsNullOrEmpty(kinopoiskId))
        {
            result.Item.SetProviderId(Constants.ProviderName, kinopoiskId);
            result.HasMetadata = true;

            await _api.Fetch(
                result, kinopoiskId, info.MetadataLanguage, info.MetadataCountryCode, cancellationToken
            ).ConfigureAwait(false);
        }

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

    public async Task<HttpResponseMessage>
    GetImageResponse(string url, CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(url, cancellationToken);

        _logger.LogInformation("GetImageResponse");

        return response;
    }
}