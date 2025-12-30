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
using Common;
using System.Linq;

public abstract class PersonProvider
(
    ILoggerFactory logger,
    IHttpClientFactory http,
    IGraphQL gql
) : BaseProvider<FilmPerson>(logger, http, gql)
{
    public override Task<FilmPerson> GetInfoByKid(int kinopoiskId, CancellationToken cancellationToken)
    => _graphql.CallAndDeserializeApi<FilmPerson>($"person/{kinopoiskId}", cancellationToken);
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

        var metadata = (FilmPerson)await WithCache(
            $"images_{kid}",
            async () => await GetInfoByKid(kid, cancellationToken)
        );

        return metadata.GetImages()
            .Where(x => !string.IsNullOrWhiteSpace(x.Item2))
            .Select(x => new RemoteImageInfo()
                {
                    Type = x.Item1,
                    Url = x.Item2,
                    Language = Constants.ProviderMetadataLanguage,
                    ProviderName = Constants.ProviderName,
                }
            );
    }
}