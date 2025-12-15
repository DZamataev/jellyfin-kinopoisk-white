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

        var kinopoiskId = info.GetProviderId(Constants.ProviderName);
        if (string.IsNullOrWhiteSpace(kinopoiskId))
        {
            _logger.LogDebug("KID is empty {item}", info.Name);
            result.QueriedById = false;

            try {
                kinopoiskId = await _api.GetKinopoiskId(info.Path, cancellationToken)
                    .ConfigureAwait(false);
                _logger.LogInformation("Found KID {kid} for {item}", kinopoiskId, info.Name);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "KID not found for {item}", info.Name);
            }
        }

        if (!string.IsNullOrEmpty(kinopoiskId))
        {
            result.Item.SetProviderId(Constants.ProviderName, kinopoiskId);
            result.HasMetadata = true;

            try {
                var metadata = await _api.Fetch(kinopoiskId, cancellationToken).ConfigureAwait(false);
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

    private static void Fill<T>(FilmInfo film, MetadataResult<T> target) where T : BaseItem
    {
        target.Item.SetProviderId(Constants.ProviderId, System.Convert.ToString(film.Id));
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

        void AddPerson(PersonKind Type, FilmInfo.FilmCrewMembers.FilmCrewMember Crew)
        {
            if (Crew?.Person?.Name == null) return;
            target.AddPerson(new PersonInfo { Name = Crew.Person.Name, Type = Type });
        }

        foreach (var person in film.Actors.Items) AddPerson(PersonKind.Actor, person);
        foreach (var person in film.Directors.Items) AddPerson(PersonKind.Director, person);
        foreach (var person in film.Writers.Items) AddPerson(PersonKind.Writer, person);
        foreach (var person in film.Producers.Items) AddPerson(PersonKind.Producer, person);
        foreach (var person in film.Composers.Items) AddPerson(PersonKind.Composer, person);
        foreach (var person in film.FilmEditors.Items) AddPerson(PersonKind.Editor, person);

        return;
    }
}