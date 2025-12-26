using Xunit;

using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Entities.Movies;

using KinopoiskWhite.Api;
using KinopoiskWhite.Api.Models;
using KinopoiskWhite.Providers;
using KinopoiskWhite.Extensions;

namespace Test;

using Common;
using KinopoiskWhite.Common;
using IMovieMetadataProvider = KinopoiskWhite.Providers.Interfaces.IMetadataProvider
    <Movie, MovieInfo, FilmInfo>;

class MockMovieMetadataProvider(
    ILoggerFactory logger,
    IHttpClientFactory http,
    IGraphQL gql
) :
    MovieMetadataProvider(logger, http, gql),
    IRemoteMetadataProvider<Movie, MovieInfo>
{
    public async Task<MetadataResult<Movie>>
    MockResolveInfo(MovieInfo info, CancellationToken cancellationToken)
    => await ((IMovieMetadataProvider)this).ResolveInfo(info, cancellationToken);

    public async Task<MetadataResult<Movie>>
    MockGetMetadata (MovieInfo info, CancellationToken cancellationToken)
    => await ((IMovieMetadataProvider)this).GetMetadata(info, cancellationToken);
}

[CollectionDefinition("Sequential", DisableParallelization = true)]
public class SequentialCollection { }

[Collection("Sequential")]
public class MovieProviderTests
{
    readonly MockMovieMetadataProvider metadataProvider;
    readonly CancellationToken token = CancellationToken.None;
    readonly MockHttpClientFactory.MessageHandler handler = new();

    public MovieProviderTests()
    {
        var factory = new MockHttpClientFactory(handler);
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IHttpClientFactory>(factory);
        services.AddSingleton<IGraphQL, GraphQL>();
        services.AddSingleton<MockMovieMetadataProvider>();

        var sp = services.BuildServiceProvider();

        metadataProvider = sp.GetRequiredService<MockMovieMetadataProvider>();
    }

    readonly FilmInfo OrigFilmInfo = new()
    {
        Id = 123,
        Title = new()
        {
            Russian = "Тест",
            Original = "Test"
        },
        ProductionYear = 1985,
    };

    static HttpResponseMessage SuggestSearchResponse(FilmInfo info) => new()
    {
        StatusCode = System.Net.HttpStatusCode.OK,
        Content = JsonContent.Create(new {
            data = new { suggest = new { top = new {
                topResult = new { global = info },
                movies = new[] { new { movie = info }, }
        }}}})
    };

    static HttpResponseMessage FilmInfoResponse(FilmInfo info) => new()
    {
        StatusCode = System.Net.HttpStatusCode.OK,
        Content = JsonContent.Create(new {
            data = new { film = info }
        })
    };


    [Fact] public async Task ShouldGetResultItemById()
    {
        handler.SetResponses([ request => FilmInfoResponse(OrigFilmInfo) ]);
        var item = new MovieInfo();
        item.SetDefaultId(OrigFilmInfo.Id);
        var result = await metadataProvider.MockGetMetadata(item, token);

        Assert.Equal(OrigFilmInfo.Title.Russian, result?.Item?.Name);
    }

    [Fact] public async Task ShouldThrowByIncorrectId()
    {
        handler.SetResponses([]);
        var item = new MovieInfo();
        item.SetProviderId(Constants.ProviderId, "123abc");
        await Assert.ThrowsAsync<BaseProvider<FilmInfo>.Error.EmptySearchString>(async () =>
            await metadataProvider.MockResolveInfo(item, token)
        );
    }

    [Fact] public async Task ShouldThrowByUnknownId()
    {
        handler.SetResponses([ request => FilmInfoResponse(null) ]);
        await Assert.ThrowsAsync<GraphQL.Error.ElementIsNull>(async () =>
            await metadataProvider.Fetch(OrigFilmInfo.Id, token)
        );

    }

    [Fact] public async Task ShouldGetIdByKeyword()
    {
        handler.SetResponses([ request => SuggestSearchResponse(OrigFilmInfo) ]);
        MovieInfo item = new() { Path = "some path" };
        var result = await metadataProvider.MockResolveInfo(item, token);

        Assert.Equal(OrigFilmInfo.Title.Russian, result?.Item?.Name);
    }
}