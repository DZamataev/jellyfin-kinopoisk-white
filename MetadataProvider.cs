using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
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

    public class MetadataProvider : IRemoteMetadataProvider<Movie, MovieInfo> {

        public string Name => Constants.ProviderName;
        public string Description => Constants.ProviderDescription;

        private readonly ILogger _logger;

        public MetadataProvider(ILogger<MetadataProvider> logger) {
            _logger = logger;
        }

        (string, int?) GetTitle(string fileName) {
            var result = fileName;
            var regex = new Regex(@"^(.*?)(?:\.(\d{4}))?(?:.[^.]+)$");

            Match match = regex.Match(fileName);
            if (match.Success) {
                string title = match.Groups[1].Value;
                int? year = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : null;
                return (title, year);
            }
            return (fileName, null);
        }
    
        public /* async */ Task<MetadataResult<Movie>>
        GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"GetMetadata {info.Name}");

            var result = new MetadataResult<Movie>() {
                QueriedById = true,
                Provider = Constants.ProviderName,
                ResultLanguage = Constants.ProviderMetadataLanguage
            };

            var (title, year) = GetTitle(Path.GetFileName(info.Path));

            result.Item = new Movie {
                Id = Guid.NewGuid(),
                Name = title + "(Test Movie)",
                Overview = "Test description с кириллицей",
                ProductionYear = year,
                DateCreated = DateTime.UtcNow,
            };

            if (result.Item != null)
                result.HasMetadata = true;

            var json = new JsonSerializerOptions { WriteIndented = true };
            _logger.LogInformation(JsonSerializer.Serialize(info, json));
            _logger.LogInformation(JsonSerializer.Serialize(result, json));

            // return result;
            return Task.FromResult(result);
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