using System;
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

namespace Jellyfin.Plugin.KinopoiskWhiteList {
    public class TestMetadataProvider : IRemoteMetadataProvider<Movie, MovieInfo> {
        public string Name => "Test Provider";
        private readonly ILogger _logger;

        public TestMetadataProvider(ILogger<TestMetadataProvider> logger) {
            _logger = logger;
        }
    
        public Task<MetadataResult<Movie>>
        GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Movie>
            {
                Item = new Movie
                {
                    Name = "Test Movie",
                    Overview = "Test description",
                    ProductionYear = 2023,
                    Id = Guid.NewGuid(),
                    DateCreated = DateTime.UtcNow,
                    ProviderIds = new Dictionary<string, string> { { "TestProvider", "test-123" } },
                    SortName = "Test Movie",
                }
            };
        
            _logger.LogInformation($"GetMetadata {info.Name}");
            var json = JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true });
            _logger.LogInformation(json);

            return Task.FromResult(result);
        }
    
        public Task<IEnumerable<RemoteSearchResult>>
        GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetSearchResults");

            if (string.IsNullOrEmpty(searchInfo.Name)) {
                _logger.LogError("GetSearchResults EMPTY");
                return Task.FromResult(Enumerable.Empty<RemoteSearchResult>());
            }

            var results = new List<RemoteSearchResult>
            {
                new RemoteSearchResult
                {
                    Name = searchInfo.Name,
                    ProductionYear = searchInfo.Year ?? 2023,
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
}