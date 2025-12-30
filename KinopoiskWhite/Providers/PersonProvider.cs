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
    ILoggerFactory logger,
    IHttpClientFactory http,
    IGraphQL gql
) : BaseProvider<FilmPerson>(logger, http, gql)
{
    public override Task<FilmPerson> GetInfoByKid(int kinopoiskId, CancellationToken cancellationToken)
    => _graphql.GetPerson(kinopoiskId, cancellationToken);
}


public class PersonExternalId (ILoggerFactory logger)
: BaseSingleton(logger), IExternalIdProvider<Person>
{
    public string ExternalIdPath => "name";
}


public class PersonMetadataProvider
(
    ILoggerFactory logger,
    IHttpClientFactory http,
    IGraphQL gql
) :
    PersonProvider(logger, http, gql),
    ISearchProvider<PersonLookupInfo, FilmPerson>,
    IMetadataProvider<Person, PersonLookupInfo, FilmPerson>
{
    public string GetSearchKeyword(PersonLookupInfo info) => info.Name;
}


public class PersonImageProvider
(
    ILoggerFactory logger,
    IHttpClientFactory http,
    IGraphQL gql
) :
    PersonProvider(logger, http, gql),
    IImageProvider<Person, FilmPerson>
{
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item) => [ImageType.Primary];

    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<IEnumerable<RemoteImageInfo>>
    GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        if (!item.TryGetDefaultId(out int kid)) return [];

        var result = (FilmPerson)await WithCache(
            $"images_{kid}",
            async () => await _graphql.GetPerson(kid, cancellationToken)
        );
        return ((IFilmImageProvider<Person, FilmPerson>)this).Convert([.. result.GetImages()]);
    }
}