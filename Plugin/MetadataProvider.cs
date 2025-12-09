using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.KinopoiskWhite {

    public class MetadataProvider : IRemoteMetadataProvider<Movie, MovieInfo> {

        public string Name => Constants.ProviderName;
        public string Description => Constants.ProviderDescription;

        private readonly ILogger _logger;
        private Api api;

        public MetadataProvider(ILogger<MetadataProvider> logger) {
            _logger = logger;
            api = Api.Instance;
        }

        public async Task<MetadataResult<Movie>>
        GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"GetMetadata {info.Name}");

            var result = new MetadataResult<Movie>() {
                QueriedById = true,
                Provider = Constants.ProviderName,
                ResultLanguage = Constants.ProviderMetadataLanguage
            };

            result.Item = await api.GetMovie(Path.GetFileName(info.Path));

            // можно убрать
            if (result.Item != null)
                result.HasMetadata = true;

            var json = new JsonSerializerOptions { WriteIndented = true };
            _logger.LogInformation(JsonSerializer.Serialize(info, json));

            return result;
        }
    
        public Task<IEnumerable<RemoteSearchResult>>
        GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken) {
            _logger.LogInformation("GetSearchResults");

            if (string.IsNullOrEmpty(searchInfo.Name)) {
                _logger.LogError("GetSearchResults EMPTY");
                return Task.FromResult(Enumerable.Empty<RemoteSearchResult>());
            }

            var results = new List<RemoteSearchResult> {
                new RemoteSearchResult {
                    Name = searchInfo.Name,
                    ProductionYear = searchInfo.Year ?? 2033,
                    ProviderIds = new Dictionary<string, string> { { "TestProvider", $"test-{searchInfo.Name}" } }
                }
            };
        
            return Task.FromResult<IEnumerable<RemoteSearchResult>>(results);
        }

        public async Task<HttpResponseMessage>
        GetImageResponse(string url, CancellationToken cancellationToken) {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url, cancellationToken);

            _logger.LogInformation("GetImageResponse");

            return response;
        }
    }
}