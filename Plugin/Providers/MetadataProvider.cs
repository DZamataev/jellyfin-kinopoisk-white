using System.Linq;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using Jellyfin.Data.Enums;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Entities.Movies;

namespace Plugin.Providers;
using Api;
using Common;

public class KinopoiskItemProvider : IRemoteMetadataProvider<Movie, MovieInfo>
{
    public string Name => Constants.ProviderName;
    public static string Description => Constants.ProviderDescription;

    private readonly ILogger _logger;
    private readonly KinopoiskApi _api;

    public KinopoiskItemProvider(ILogger<KinopoiskItemProvider> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _api = new KinopoiskApi(httpClientFactory);
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

        var contentId = info.GetProviderId(Constants.ProviderId);

        if (string.IsNullOrWhiteSpace(contentId))
        {
            _logger.LogDebug("KID is empty {item}", info.Name);
            result.QueriedById = false;

            try {
                var meta = await _api.GetKinopoiskId(info.Path, cancellationToken)
                    .ConfigureAwait(false);
                _logger.LogInformation("Found KID {kid} for {item}", meta.Kid, info.Name);

                contentId = meta.ContentId;
                result.Item.SetProviderId(Constants.ProviderId, meta.ContentId);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "KID not found for {item}", info.Name);
            }
        }

        if (!string.IsNullOrEmpty(contentId))
        {
            try {
                var metadata = await _api.Fetch(contentId, cancellationToken).ConfigureAwait(false);
                Fill(metadata, result);
                _logger.LogInformation("Metadata loaded for {info}", info.Name);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Metadata not found for {item}", info.Name);
            }
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

    public static void Fill<T>(FilmInfo film, MetadataResult<T> target) where T : BaseItem
    {
        target.Item.SetProviderId(Constants.ProviderName, film.Kid);
        target.Item.Name = film.Title.Russian;
        target.Item.OriginalTitle = film.Title.Original;
        target.Item.ProductionYear = film.ProductionYear;
        target.Item.CommunityRating = film.Rating.Community;
        target.Item.CriticRating = film.Rating.Critics;
        target.Item.CustomRating = film.Restriction?.Rating;

        target.Item.Tagline = film.ShortDescription;
        target.Item.Overview = film.Synopsis;

        foreach (var genre in film.Genres)
            target.Item.AddGenre(genre.Slug);

        void AddCrew(PersonKind Type, FilmInfo.FilmCrewMembers members)
        {
            foreach (var crew in members?.Items ?? [])
            {
                if (crew?.Person?.Name == null) return;
                target.AddPerson(new PersonInfo { Name = crew.Person.Name, Type = Type });
            }
        }

        AddCrew(PersonKind.Actor, film.Actors);
        AddCrew(PersonKind.Director, film.Directors);
        AddCrew(PersonKind.Writer, film.Writers);
        AddCrew(PersonKind.Producer, film.Producers);
        AddCrew(PersonKind.Composer, film.Composers);
        AddCrew(PersonKind.Editor, film.FilmEditors);

        target.HasMetadata = true;

        return;
    }
}