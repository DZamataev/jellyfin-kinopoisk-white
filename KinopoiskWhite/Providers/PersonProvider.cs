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

using Api.Models;
using Extensions;
using Interfaces;

public abstract class PersonProvider(ILoggerFactory logger, IHttpClientFactory http)
: BaseProvider<FilmPerson>(logger, http)
{
    protected override async Task<FilmPerson>
    FetchAsync(int kinopoiskId, CancellationToken cancellationToken)
    => await _graphql.GetPerson(kinopoiskId, cancellationToken);
}


public class PersonExternalId (ILoggerFactory logger)
: BaseSingleton(logger), IExternalIdProvider<Person>
{
    public string ExternalIdPath => "name";
}


public class PersonMetadataProvider(ILoggerFactory logger, IHttpClientFactory http)
:
    PersonProvider(logger, http),
    ISearchProvider<PersonLookupInfo, FilmPerson>,
    IMetadataProvider<Person, PersonLookupInfo, FilmPerson>
{
    public string GetSearchKeyword(PersonLookupInfo info) => info.Name;
}


public class PersonImageProvider(ILoggerFactory logger, IHttpClientFactory http)
:
    PersonProvider(logger, http),
    IImageProvider<Person, FilmPerson>
{
    private readonly Dictionary<int, FilmPerson> _cache = [];
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
            _logger.LogDebug("Get images {kid}", kid);
            result = await _graphql.GetPerson(kid, cancellationToken).ConfigureAwait(false);
            _cache[kid] = result ?? throw new System.Exception(
                $"Get Kinopoisk metadata failed KID {kid}");
        }
        return result.GetImages();
    }
}