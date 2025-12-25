using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

namespace KinopoiskWhite.Providers;

using Api;
using Api.Models;
using Extensions;
using Interfaces;

public abstract class PersonProvider
(
    ILogger<PersonProvider> logger,
    IHttpClientFactory httpClientFactory,
    IGraphQL graphQL
) :
    BaseProvider<FilmPerson>(logger, httpClientFactory, graphQL),
    ISearchProvider<PersonLookupInfo, FilmPerson>,
    IMetadataProvider<Person, PersonLookupInfo, FilmPerson>,
    IImageProvider<Person>
{
    private readonly Dictionary<int, FilmPerson> _cache = [];
    public bool Supports(BaseItem item) => item is Person;
    public string GetSearchKeyword(PersonLookupInfo info) => info.Name;
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item) => [ImageType.Primary];

    public async Task<IEnumerable<RemoteImageInfo>>
    GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        if (!item.TryGetDefaultId(out int kid)) return [];

        _logger.LogDebug("Getting images for {name}", item.Name);

        if (_cache.TryGetValue(kid, out FilmPerson result))
            _logger.LogDebug("Getting cached by {kid}", kid);

        else
        {
            result = await GetImages(kid, cancellationToken).ConfigureAwait(false);
            _cache[kid] = result;
        }
        return result.GetImages();
    }

    protected override async Task<FilmPerson>
    FetchAsync(int kinopoiskId, CancellationToken cancellationToken)
    => await _graphql.GetPerson(kinopoiskId, cancellationToken);

    protected override async Task<FilmPerson>
    GetImagesAsync(int kinopoiskId, CancellationToken cancellationToken)
    => await _graphql.GetPerson(kinopoiskId, cancellationToken);
}