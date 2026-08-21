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
using KinopoiskWhite.Providers.Interfaces;
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

    static int Kid = 100;
    static FilmInfo OrigFilmInfo => new()
    {
        Id = Kid,
        ContentId = $"{Kid++}abc",
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
    static FilmInfo OrigFilmInfoWithGallery => OrigFilmInfo with
    {
        Gallery = new()
        {
            Posters = new() { Vertical = new() { AvatarsUrl = "//posters" } },
            Logos = new() { Horizontal = new() { AvatarsUrl = "//logos" } },
        }
    };

    static FilmInfo OrigFilmInfoWithImages => OrigFilmInfo with
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
        var info = OrigFilmInfo;
        httpFactory.SetResponses([ FilmInfoResponse(info) ]);
        var item = new MovieInfo();
        item.SetDefaultId(info.Id);
        var result = await metadataProvider.MockGetMetadata(item, token);

        Assert.Equal(info.Title.Russian, result?.Item?.Name);
    }

    [Fact] public async Task ShouldThrowByIncorrectId()
    {
        httpFactory.SetResponses([]);
        var item = new MovieInfo();
        item.SetProviderId(Constants.ProviderId, "123abc");
        await Assert.ThrowsAsync<BaseProvider<FilmInfo>.Error.EmptySearchString>(async () =>
            await metadataProvider.MockResolveInfo(item, token)
        );
    }

    [Fact] public async Task ShouldThrowByUnknownId()
    {
        var info = OrigFilmInfo;
        httpFactory.SetResponses([ FilmInfoResponse(null) ]);
        await Assert.ThrowsAsync<BaseProvider<FilmInfo>.Error.GettingRemote>(async () =>
            await metadataProvider.Fetch(info.Id, token)
        );

    }

    [Fact] public async Task ShouldGetIdByKeyword()
    {
        var info = OrigFilmInfo;
        httpFactory.SetResponses([ SuggestSearchResponse(info) ]);
        MovieInfo item = new() { Path = "some path" };
        var result = await metadataProvider.MockResolveInfo(item, token);

        Assert.Equal(info.Title.Russian, result?.Item?.Name);
    }

    // A suggest response where the FIRST (top) result is the wrong year and the
    // right-year candidate is further down the list.
    static HttpResponseMessage SuggestTwo(FilmInfo top, FilmInfo other) => new()
    {
        StatusCode = System.Net.HttpStatusCode.OK,
        Content = JsonContent.Create(new {
            data = new { suggest = new { top = new {
                topResult = new { global = top },
                movies = new[] { new { movie = other } },
        }}}})
    };

    [Fact] public async Task ShouldPreferYearMatchOverFirstResult()
    {
        var wrong = new FilmInfo { Id = 111, Title = new() { Russian = "Радуга (фильм)", Original = "R" }, ProductionYear = 2010 };
        var right = new FilmInfo { Id = 222, Title = new() { Russian = "Радуга (мультфильм)", Original = "R" }, ProductionYear = 1975 };
        httpFactory.SetResponses([ SuggestTwo(wrong, right) ]);

        MovieInfo item = new() { Path = "Радуга", Year = 1975 };
        var result = await metadataProvider.MockResolveInfo(item, token);

        Assert.Equal("Радуга (мультфильм)", result?.Item?.Name);
        Assert.Equal(222, item.GetDefaultId());
    }

    [Fact] public async Task ShouldFallBackToFirstWhenNoYearMatches()
    {
        var first = new FilmInfo { Id = 111, Title = new() { Russian = "Первый", Original = "F" }, ProductionYear = 2000 };
        var second = new FilmInfo { Id = 222, Title = new() { Russian = "Второй", Original = "S" }, ProductionYear = 2001 };
        httpFactory.SetResponses([ SuggestTwo(first, second) ]);

        MovieInfo item = new() { Path = "Кино", Year = 1930 };
        var result = await metadataProvider.MockResolveInfo(item, token);

        Assert.Equal("Первый", result?.Item?.Name);
    }

    [Fact] public async Task ShouldTakeFirstWhenNoYearGiven()
    {
        var first = new FilmInfo { Id = 111, Title = new() { Russian = "Первый", Original = "F" }, ProductionYear = 2000 };
        var second = new FilmInfo { Id = 222, Title = new() { Russian = "Второй", Original = "S" }, ProductionYear = 1975 };
        httpFactory.SetResponses([ SuggestTwo(first, second) ]);

        MovieInfo item = new() { Path = "Кино" }; // no Year
        var result = await metadataProvider.MockResolveInfo(item, token);

        Assert.Equal("Первый", result?.Item?.Name);
    }

    [Fact]
    public async Task ShouldGetImagesByKid()
    {
        var info = OrigFilmInfo;
        httpFactory.SetResponses([ FilmInfoResponse(info) ]);
        var item = new Movie();
        item.SetDefaultId(info.Id);

        var results = await ((IImageProvider<Movie, FilmInfo>)imageProvider).GetImages(item, token);

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

    [Fact]
    public async Task ShouldGetImagesByContentId()
    {
        var info = OrigFilmInfoWithGallery;
        httpFactory.SetResponses([
            FilmInfoResponse(info),
            FilmPageResponse(info)
        ]);
        var item = new Movie();
        item.SetDefaultId(info.Id);
        item.SetContentId(info.ContentId);

        var results = await ((IImageProvider<Movie, FilmInfo>)imageProvider).GetImages(item, token);

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

    [Fact]
    public async Task ShouldGetImagesWithoutContentId()
    {
        var info = OrigFilmInfo;
        httpFactory.SetResponses([
            FilmInfoResponse(info),
            MovieImagesItemsResponse(OrigFilmInfoWithImages),
        ]);
        var item = new Movie();
        item.SetDefaultId(info.Id);

        var results = await ((IImageProvider<Movie, FilmInfo>)imageProvider).GetImages(item, token);

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

    [Fact] public async Task ShouldGetFullImages()
    {
        var info = OrigFilmInfoWithGallery;
        httpFactory.SetResponses([
            FilmInfoResponse(info),
            FilmPageResponse(info),
            MovieImagesItemsResponse(OrigFilmInfoWithImages),
        ]);
        var item = new Movie();
        item.SetDefaultId(info.Id);
        item.SetContentId(info.ContentId);

        var results = await ((IImageProvider<Movie, FilmInfo>)imageProvider).GetImages(item, token);

        HashSet<string> expected =
        [
            "https://marketing/576x",
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