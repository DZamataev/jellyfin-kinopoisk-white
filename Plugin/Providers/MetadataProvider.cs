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

public class KinopoiskItemProvider : IRemoteMetadataProvider<Movie, MovieInfo>,
                                     IRemoteImageProvider
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

    public IEnumerable<ImageType> GetSupportedImages(BaseItem item) =>
    [
        ImageType.Primary,
        ImageType.Backdrop,
        ImageType.Logo,
    ];

    public bool Supports(BaseItem item) => item is Movie;

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

        var kid = info.GetProviderId(Constants.ProviderName);

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

        Fill(meta, result);

        _logger.LogInformation("Metadata loaded for {kid}", kid);

        return result;
    }

    public async Task<IEnumerable<RemoteImageInfo>>
    GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        var cid = item.GetProviderId(Constants.ProviderId);
        var kid = item.GetProviderId(Constants.ProviderName);

        if (string.IsNullOrWhiteSpace(kid)) return [];

        FilmInfo meta = null;
        if (!string.IsNullOrWhiteSpace(cid))
        {
            meta = await _api.FetchByCid(cid, cancellationToken).ConfigureAwait(false);
        }
        if (meta == null)
        {
            meta = await _api.FetchByKid(kid, cancellationToken).ConfigureAwait(false);
        }

        return FillImages(meta);
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

    public static void
    Fill<T>(FilmInfo film, MetadataResult<T> target) where T : BaseItem
    {
        target.Item.SetProviderId(Constants.ProviderName, film.Kid);

        if (!string.IsNullOrEmpty(film.ContentId))
            target.Item.SetProviderId(Constants.ProviderId, film.ContentId);

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

    private static IEnumerable<RemoteImageInfo>
    FillImages(FilmInfo film)
    {
        var res = Enumerable.Empty<RemoteImageInfo>();

        static RemoteImageInfo fill(ImageType type, string image)
        {
            if (image == null) return null;

            return new RemoteImageInfo
            {
                Type = type,
                Url = image,
                Language = Constants.ProviderMetadataLanguage,
                ProviderName = Constants.ProviderName,
            };
        }

        (ImageType, string)[] images = [
            (ImageType.Primary, film.Gallery?.Primary),
            (ImageType.Backdrop, film.Gallery?.Backdrop),
            (ImageType.Logo, film.Gallery?.Logo),
        ];

        foreach (var (type, url) in images)
        {
            var result = fill(type, url);

            if (result != null)
                yield return result;
        }
    }
}