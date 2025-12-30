using Xunit;

using System.Net.Http;
using System.Text.Json;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using KinopoiskWhite.Api;
using KinopoiskWhite.Api.Models;

namespace Test;
using Common;

public class GraphQlTests {
    private readonly MockGraphQL graphql;
    readonly MockHttpClientFactory httpFactory = new();
    private readonly CancellationToken token = new();

    private readonly FilmInfo origFilmInfo = new()
    {
        Id = 11,
        Title = new()
        {
            Original = "Test"
        },
        ProductionYear = 1985,
    };

    public GraphQlTests()
    {
        var logger = LoggerFactory.Create(builder => builder.AddDebug());
        graphql = new MockGraphQL(logger, httpFactory.Object);
    }

    [Theory]
    [InlineData("FilmPage")]
    [InlineData("FilmBaseInfo")]
    [InlineData("MovieImagesItems")]
    [InlineData("SuggestSearch")]
    public void ShouldGetEmbeddedQuery(string fileName)
    {
        var result = MockGraphQL.MockGetEmbeddedQuery(fileName);
        result = result.Split("(")[0];
        Assert.Equal(result, $"query {fileName}");
    }

    [Fact]
    public void ShouldThrowAnExceptionOnEmbeddedQuery()
    => Assert.Throws<System.IO.FileNotFoundException>(()
    => MockGraphQL.MockGetEmbeddedQuery("UnknownQuery"));

    [Fact]
    public async Task ShouldCall()
    {
        httpFactory.SetResponse( new() { Content = new StringContent("{}") });

        var root = await graphql.MockCall("SuggestSearch", new(), token);
        Assert.Equal(JsonValueKind.Object, root.ValueKind);
    }

    [Fact]
    public async Task ShouldCallApi()
    {
        httpFactory.SetResponse( new() { Content = new StringContent("{}") });

        var root = await graphql.MockCallApi("test", token);
        Assert.Equal(JsonValueKind.Object, root.ValueKind);
    }

    [Fact]
    public async Task CallApiShouldThrowAnExceptionOnNull()
    {
        httpFactory.SetResponse( new() { Content = null });

        await Assert.ThrowsAsync<GraphQL.Error.DocumentIsNull>(async () =>
            await graphql.MockCallApi("test", token)
        );
    }

    [Fact]
    public async Task CallApiShouldThrowAnExceptionOnWrongResponse()
    {
        httpFactory.SetResponse( new() { Content = new StringContent("NOT JSON") });

        await Assert.ThrowsAsync<GraphQL.Error.DocumentInvalid>(async () =>
            await graphql.MockCallApi("test", token)
        );
    }

    [Fact]
    public void ShouldWalk()
    {
        var example = new { child1 = new { child2 = new { value = 11 } } };
        var root = JsonDocument.Parse(JsonSerializer.Serialize(example)).RootElement;

        var result = MockGraphQL.MockWalk(root, "child1.child2.value");
        Assert.Equal(example.child1.child2.value, result.GetInt32());
    }

    [Fact]
    public async Task ShouldCallAndWalk()
    {
        var example = new { child1 = new { child2 = new { value = 11 } } };
        httpFactory.SetResponse( new() { Content = JsonContent.Create(example) });

        var result = await graphql.MockCall("SuggestSearch", new(), "child1.child2.value", token);
        Assert.Equal(example.child1.child2.value, result.GetInt32());
    }

    [Fact]
    public async Task ShouldCallAndDeserialize()
    {
        var example = new { child1 = new { child2 = new { value = origFilmInfo } } };

        httpFactory.SetResponse( new() { Content = JsonContent.Create(example) });

        var result = await graphql.MockCallAndDeserialize("SuggestSearch", new(), "child1.child2.value", token);
        Assert.Equal(origFilmInfo.Id, result.Id);
        Assert.Equal(origFilmInfo.Title.Original, result.Title.Original);
    }

    [Fact]
    public async Task SuggestSearchShouldReturnResult()
    {
        var data = new { data = new { suggest = new { top = new {
                topResult = new { global = origFilmInfo },
                movies = new[] { new { movie = origFilmInfo }, }
        }}}};

        httpFactory.SetResponse( new() { Content = JsonContent.Create(data) });

        var count = 0;
        await foreach (var film in graphql.SuggestSearch<FilmInfo>("keyword", token))
        {
            Assert.Equal(origFilmInfo.Id, film.Id);
            Assert.Equal(origFilmInfo.Title.Original, film.Title.Original);
            Assert.Equal(origFilmInfo.ProductionYear, film.ProductionYear);
            count ++;
        }
        Assert.Equal(2, count);
    }
}