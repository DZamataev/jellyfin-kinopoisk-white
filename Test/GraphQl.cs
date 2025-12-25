using Xunit;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using Moq;
using Moq.Protected;

using KinopoiskWhite.Api;
using KinopoiskWhite.Api.Models;

namespace Test; 


class MockGraphQL(
    ILoggerFactory loggerFactory,
    IHttpClientFactory httpClientFactory
) : GraphQL(loggerFactory, httpClientFactory)
{
    public static string MockGetEmbeddedQuery(string fileName)
    => GetEmbeddedQuery(fileName);

    public async Task<JsonElement> MockCall(string operationName, object variables,
                                            CancellationToken cancellationToken)
    => await Call(operationName, variables, cancellationToken);

    public async Task<JsonElement> MockCallApi(string method, CancellationToken cancellationToken)
    => await CallApi(method, cancellationToken);

    public static JsonElement MockWalk(JsonElement root, string path) => Walk(root, path);

    public async Task<JsonElement> MockCall(string operationName, object variables, string path,
                                            CancellationToken cancellationToken)
    => await Call(operationName, variables, path, cancellationToken);

    public async Task<FilmInfo> MockCallAndDeserialize(
        string operationName, object variables, string path,
        CancellationToken cancellationToken)
    => await CallAndDeserialize(operationName, variables, path, cancellationToken);
}


public class GraphQlTests {
    private readonly MockGraphQL graphql;
    private readonly CancellationToken token = new();

    private static readonly HttpResponseMessage response = new()
    {
        StatusCode = HttpStatusCode.OK,
        Content = new StringContent("Mocked response")
    };

    private static HttpClient HttpClient {
        get {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(response);

            return new HttpClient(handler.Object);
        }
    }
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
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(HttpClient);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpClientFactory>(factoryMock.Object);
        MockGraphQL.RegisterServices(services);
        var sp = services.BuildServiceProvider();

        graphql = new MockGraphQL(
            new LoggerFactory(),
            sp.GetRequiredService<IHttpClientFactory>()
        );
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
        response.Content = new StringContent("{}");

        var root = await graphql.MockCall("SuggestSearch", new(), token);
        Assert.Equal(JsonValueKind.Object, root.ValueKind);
    }

    [Fact]
    public async Task ShouldCallApi()
    {
        response.Content = new StringContent("{}");

        var root = await graphql.MockCallApi("test", token);
        Assert.Equal(JsonValueKind.Object, root.ValueKind);
    }

    [Fact]
    public async Task CallApiShouldThrowAnExceptionOnNull()
    {
        response.Content = null;

        var ex = await Assert.ThrowsAsync<System.Exception>(async () =>
            await graphql.MockCallApi("test", token)
        );
        Assert.Equal("Document is null", ex.Message);
    }

    [Fact]
    public async Task CallApiShouldThrowAnExceptionOnWrongResponse()
    {
        response.Content = new StringContent("NOT JSON");

        var ex = await Assert.ThrowsAsync<System.Exception>(async () =>
            await graphql.MockCallApi("test", token)
        );
        Assert.Equal("Invalid document", ex.Message);
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
        response.Content = JsonContent.Create(example);

        var result = await graphql.MockCall("SuggestSearch", new(), "child1.child2.value", token);
        Assert.Equal(example.child1.child2.value, result.GetInt32());
    }

    [Fact]
    public async Task ShouldCallAndDeserialize()
    {
        var example = new { child1 = new { child2 = new { value = origFilmInfo } } };

        response.Content = JsonContent.Create(example);

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

        response.Content = JsonContent.Create(data);

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

    [Fact]
    public async Task FilmBaseInfoShouldReturnResult()
    {
        var data = new { data = new { film = origFilmInfo }};
        response.Content = JsonContent.Create(data);

        var film = await graphql.FilmBaseInfo(origFilmInfo.Id, token);

        Assert.Equal(origFilmInfo.Id, film.Id);
        Assert.Equal(origFilmInfo.Title.Original, film.Title.Original);
        Assert.Equal(origFilmInfo.ProductionYear, film.ProductionYear);
    }

    [Fact]
    public async Task FilmPageShouldReturnResult()
    {
        var data = new { data = new { movieByContentUuid = origFilmInfo }};
        response.Content = JsonContent.Create(data);

        var film = await graphql.FilmPage("contentId", token);

        Assert.Equal(origFilmInfo.Id, film.Id);
        Assert.Equal(origFilmInfo.Title.Original, film.Title.Original);
        Assert.Equal(origFilmInfo.ProductionYear, film.ProductionYear);
    }

    [Fact]
    public async Task MovieImagesItemsShouldReturnResult()
    {
        var data = new { data = new { movie = origFilmInfo }};
        response.Content = JsonContent.Create(data);

        var film = await graphql.MovieImagesItems(origFilmInfo.Id, FilmImageType.POSTER, token);

        Assert.Equal(origFilmInfo.Id, film.Id);
        Assert.Equal(origFilmInfo.Title.Original, film.Title.Original);
        Assert.Equal(origFilmInfo.ProductionYear, film.ProductionYear);
    }

    [Fact]
    public async Task GetPersonShouldReturnResult()
    {
        FilmPerson origPerson = new()
        {
            Id = 123,
            Name = "Some Person"
        };
        response.Content = JsonContent.Create(origPerson);

        var person = await graphql.GetPerson(origPerson.Id, token);

        Assert.Equal(origPerson.Id, person.Id);
        Assert.Equal(origPerson.Name, person.Name);
    }
}

/*

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHttpClientFactory>(_clientFactoryMock.Object);

        var sp = services.BuildServiceProvider();
        graphql = sp.GetRequiredService<IGraphQL>();

        _api = new ApiServiceMovie(
            LoggerFactory.Create(f => f.AddDebug()).CreateLogger<ApiServiceMovie>()
        );
*/