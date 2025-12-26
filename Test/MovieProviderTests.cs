using Xunit;

using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Entities.Movies;

using KinopoiskWhite.Api;
using KinopoiskWhite.Api.Models;
using KinopoiskWhite.Providers;
using KinopoiskWhite.Extensions;

namespace Test;

using Common;
using KinopoiskWhite.Common;
using MediaBrowser.Model.Entities;

class MockMovieMetadataProvider(
    ILoggerFactory logger,
    IHttpClientFactory http,
    IGraphQL gql
) :
    MovieMetadataProvider(logger, http, gql),
    IRemoteMetadataProvider<Movie, MovieInfo>
{}

public class MovieProviderTests
{
    readonly MockMovieMetadataProvider provider;
    readonly CancellationToken token = CancellationToken.None;
    readonly MockHttpClientFactory.MessageHandler handler = new();
    IRemoteMetadataProvider<Movie, MovieInfo> Iprovider => provider;

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

        provider = sp.GetRequiredService<MockMovieMetadataProvider>();
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

    HttpResponseMessage SuggestSearchResponse(FilmInfo info) => new()
    {
        StatusCode = System.Net.HttpStatusCode.OK,
        Content = JsonContent.Create(new {
            data = new { suggest = new { top = new {
                topResult = new { global = info },
                movies = new[] { new { movie = info }, }
        }}}})
    };

    HttpResponseMessage FilmInfoResponse(FilmInfo info) => new()
    {
        StatusCode = System.Net.HttpStatusCode.OK,
        Content = JsonContent.Create(new {
            data = new { film = info }
        })
    };


    [Fact] public async Task ShouldGetResultItemById()
    {
        handler.Responses = new([ request => FilmInfoResponse(OrigFilmInfo) ]);
        var item = new MovieInfo();
        item.SetDefaultId(OrigFilmInfo.Id);
        var result = await Iprovider.GetMetadata(item, token);

        Assert.Equal(OrigFilmInfo.Title.Russian, result?.Item?.Name);
    }

    [Fact] public async Task ShouldReturnNullByIncorrectId()
    {
        var item = new MovieInfo();
        item.SetProviderId(Constants.ProviderId, "123abc");
        var result = await Iprovider.GetMetadata(item, token);

        Assert.Null(result);
    }

    [Fact] public async Task ShouldReturnNullByUnknownId()
    {
        handler.Responses = new([ request => FilmInfoResponse(null) ]);
        var item = new MovieInfo();
        item.SetDefaultId(OrigFilmInfo.Id);
        var result = await Iprovider.GetMetadata(item, token);

        // Assert.Null(result);
    }

    [Fact] public async Task ShouldGetIdByKeyword()
    {
        handler.Responses = new([ request => SuggestSearchResponse(OrigFilmInfo) ]);
        var result = await provider.GetKinopoiskId("keyword", token);
        Assert.Equal(OrigFilmInfo.Id, result.Id);
    }
}