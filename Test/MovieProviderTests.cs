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