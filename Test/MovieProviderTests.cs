using Moq;
using Xunit;

using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Entities.Movies;

using KinopoiskWhite.Api;
using KinopoiskWhite.Api.Models;
using KinopoiskWhite.Common;
using KinopoiskWhite.Providers;
using KinopoiskWhite.Extensions;

namespace Test;

using Common;

using IMovieMetadataProvider = KinopoiskWhite.Providers.Interfaces.IMetadataProvider
    <Movie, MovieInfo, FilmInfo>;

class MockMovieMetadataProvider(
    ILoggerFactory logger,
    IHttpClientFactory http,
    IGraphQL gql
) : MovieMetadataProvider(logger, http, gql)
{
    public async Task<MetadataResult<Movie>>
    MockResolveInfo(MovieInfo info, CancellationToken cancellationToken)
    => await ((IMovieMetadataProvider)this).ResolveInfo(info, cancellationToken);

    public async Task<MetadataResult<Movie>>
    MockGetMetadata (MovieInfo info, CancellationToken cancellationToken)
    => await ((IMovieMetadataProvider)this).GetMetadata(info, cancellationToken);
}

class MockMovieImageProvider(
    ILoggerFactory logger,
    IHttpClientFactory http,
    IGraphQL gql
) : MovieImageProvider(logger, http, gql);

// [CollectionDefinition("Sequential", DisableParallelization = true)]
// public class SequentialCollection { }

// [Collection("Sequential")]
public class MovieProviderTests
{
    readonly MockMovieMetadataProvider metadataProvider;
    readonly MockMovieImageProvider imageProvider;
    readonly CancellationToken token = CancellationToken.None;
    readonly MockHttpClientFactory httpFactory = new();

    public MovieProviderTests()
    {
        var logger = LoggerFactory.Create(builder => builder.AddDebug());
        var graphql = new MockGraphQL(logger, httpFactory.Object);

        metadataProvider = new MockMovieMetadataProvider(logger, httpFactory.Object, graphql);
        imageProvider = new MockMovieImageProvider(logger, httpFactory.Object, graphql);
    }

    readonly FilmInfo OrigFilmInfo = new()
    {
        Id = 123,
        ContentId = "123abc",
        Title = new()
        {
            Russian = "Тест",
            Original = "Test"
        },
        Gallery = new()
        {
            Posters = new() { MarketingVertical= new() { AvatarsUrl = "//marketing" } },
        },
        ProductionYear = 1985,
    };
    FilmInfo OrigFilmInfoWithGallery => OrigFilmInfo with
    {
        Gallery = new()
        {
            Posters = new() { Vertical = new() { AvatarsUrl = "//posters" } },
            Logos = new() { Horizontal = new() { AvatarsUrl = "//logos" } },
        }
    };

    FilmInfo OrigFilmInfoWithImages => OrigFilmInfo with
    {
        Images = new()
        {
            Items = [
                new() { Type = FilmImageType.POSTER, Image = new () { AvatarsUrl = "//poster" }},
                new() { Type = FilmImageType.COVER, Image = new () { AvatarsUrl = "//cover" }},
                new() { Type = FilmImageType.WALLPAPER, Image = new () { AvatarsUrl = "//wallpaper" }},
            ]
        }
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
        Content = JsonContent.Create(new { data = new { film = info } })
    };

    static HttpResponseMessage FilmPageResponse(FilmInfo info) => new()
    {
        StatusCode = System.Net.HttpStatusCode.OK,
        Content = JsonContent.Create(new { data = new { movieByContentUuid = info } })
    };

    static HttpResponseMessage MovieImagesItemsResponse(FilmInfo info) => new()
    {
        StatusCode = System.Net.HttpStatusCode.OK,
        Content = JsonContent.Create(new { data = new { movie = info } })
    };


    [Fact] public async Task ShouldGetResultItemById()
    {
        MockHttpClientFactory.SetResponses([ FilmInfoResponse(OrigFilmInfo) ]);
        var item = new MovieInfo();
        item.SetDefaultId(OrigFilmInfo.Id);
        var result = await metadataProvider.MockGetMetadata(item, token);

        Assert.Equal(OrigFilmInfo.Title.Russian, result?.Item?.Name);
    }

    [Fact] public async Task ShouldThrowByIncorrectId()
    {
        MockHttpClientFactory.SetResponses([]);
        var item = new MovieInfo();
        item.SetProviderId(Constants.ProviderId, "123abc");
        await Assert.ThrowsAsync<BaseProvider<FilmInfo>.Error.EmptySearchString>(async () =>
            await metadataProvider.MockResolveInfo(item, token)
        );
    }

    [Fact] public async Task ShouldThrowByUnknownId()
    {
        MockHttpClientFactory.SetResponses([ FilmInfoResponse(null) ]);
        await Assert.ThrowsAsync<GraphQL.Error.ElementIsNull>(async () =>
            await metadataProvider.Fetch(OrigFilmInfo.Id, token)
        );

    }

    [Fact] public async Task ShouldGetIdByKeyword()
    {
        MockHttpClientFactory.SetResponses([ SuggestSearchResponse(OrigFilmInfo) ]);
        MovieInfo item = new() { Path = "some path" };
        var result = await metadataProvider.MockResolveInfo(item, token);

        Assert.Equal(OrigFilmInfo.Title.Russian, result?.Item?.Name);
    }

    [Fact]
    public async Task ShouldGetImagesByKid()
    {
        MockHttpClientFactory.SetResponses([ FilmInfoResponse(OrigFilmInfo) ]);
        var item = new Movie();
        item.SetDefaultId(OrigFilmInfo.Id);

        var results = await imageProvider.GetImages(item, token);

        HashSet<string> expected =
        [
            "https://marketing/576x",
        ];

        Assert.NotNull(results);
        foreach (var result in results)
        {
            Assert.Contains(result.Url, expected);
            expected.Remove(result.Url);
        }
        Assert.Empty(expected);
    }

    [Fact(Skip = "http client issue")]
    public async Task ShouldGetImagesByContentId()
    {
        MockHttpClientFactory.SetResponses([ FilmPageResponse(OrigFilmInfoWithGallery) ]);
        var item = new Movie();
        item.SetDefaultId(OrigFilmInfo.Id);
        item.SetContentId(OrigFilmInfo.ContentId);

        var results = await imageProvider.GetImages(item, token);

        HashSet<string> expected =
        [
            "https://posters/576x",
            "https://logos/576x",
        ];

        Assert.NotNull(results);
        foreach (var result in results)
        {
            Assert.Contains(result.Url, expected);
            expected.Remove(result.Url);
        }
        Assert.Empty(expected);
    }

    [Fact(Skip = "http client issue")]
    public async Task ShouldGetImagesWithoutContentId()
    {
        MockHttpClientFactory.SetResponses([
            FilmInfoResponse(OrigFilmInfo),
            MovieImagesItemsResponse(OrigFilmInfoWithImages),
        ]);
        var item = new Movie();
        item.SetDefaultId(OrigFilmInfo.Id);

        var results = await imageProvider.GetImages(item, token);

        HashSet<string> expected =
        [
            "https://marketing/576x",
            "https://wallpaper/576x",
            "https://poster/576x",
            "https://cover/576x",
        ];

        Assert.NotNull(results);
        foreach (var result in results)
        {
            Assert.Contains(result.Url, expected);
            expected.Remove(result.Url);
        }
        Assert.Empty(expected);
    }

    [Fact(Skip = "http client issue")] public async Task ShouldGetFullImages()
    {
        MockHttpClientFactory.SetResponses([
            FilmPageResponse(OrigFilmInfoWithGallery),
            MovieImagesItemsResponse(OrigFilmInfoWithImages),
        ]);
        var item = new Movie();
        item.SetDefaultId(OrigFilmInfo.Id);
        item.SetContentId(OrigFilmInfo.ContentId);

        var results = await imageProvider.GetImages(item, token);

        HashSet<string> expected =
        [
            "https://posters/576x",
            "https://logos/576x",
            "https://wallpaper/576x",
            "https://poster/576x",
            "https://cover/576x",
        ];

        Assert.NotNull(results);
        foreach (var result in results)
        {
            Assert.Contains(result.Url, expected);
            expected.Remove(result.Url);
        }
        Assert.Empty(expected);
    }
}