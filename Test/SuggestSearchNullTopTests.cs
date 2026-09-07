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

// Regression tests for the 2026-09-07 outage: the KinoPoisk GraphQL API answers
// `{"errors":[{"message":"Internal server error","path":["suggest","top"]}]}` with
// `data.suggest.top = null` for roughly half of all SuggestSearch calls made without
// a `yandexCityId` variable. Measured from CT 101: 12/20 null without it, 0/20 with it.
//
// Two distinct defects surfaced:
//  1. the embedded query is called without `yandexCityId`, which triggers the API fault;
//  2. `Walk` returns a JSON null instead of throwing, so the null escapes into
//     `SuggestSearch` and blows up as InvalidOperationException from
//     JsonDocument.TryGetNamedPropertyValue.
public class SuggestSearchNullTopTests
{
    private readonly MockGraphQL graphql;
    readonly MockHttpClientFactory httpFactory = new();
    private readonly CancellationToken token = new();

    public SuggestSearchNullTopTests()
    {
        var logger = LoggerFactory.Create(builder => builder.AddDebug());
        graphql = new MockGraphQL(logger, httpFactory.Object);
    }

    // Defect 2: a null leaf must be reported as ElementIsNull, not handed back as a
    // JsonValueKind.Null element that callers then dereference.
    [Fact]
    public void WalkShouldThrowWhenLeafIsNull()
    {
        var example = new { data = new { suggest = new { top = (object)null } } };
        var root = JsonDocument.Parse(JsonSerializer.Serialize(example)).RootElement;

        Assert.Throws<GraphQL.Error.ElementIsNull>(()
            => MockGraphQL.MockWalk(root, "data.suggest.top"));
    }

    // Defect 2, as seen in production: the exact payload the API returns during the
    // fault must not produce InvalidOperationException out of SuggestSearch.
    [Fact]
    public async Task SuggestSearchShouldThrowElementIsNullOnNullTop()
    {
        var data = new
        {
            data = new { suggest = new { top = (object)null } },
            errors = new[] { new { message = "Internal server error" } },
        };

        httpFactory.SetResponse(new() { Content = JsonContent.Create(data) });

        await Assert.ThrowsAsync<GraphQL.Error.ElementIsNull>(async () =>
        {
            await foreach (var _ in graphql.SuggestSearch<FilmInfo>("keyword", token))
                Assert.Fail("Should not yield");
        });
    }

    // Defect 1: the request body must carry yandexCityId, otherwise the API faults.
    [Fact]
    public async Task SuggestSearchShouldSendYandexCityId()
    {
        var data = new
        {
            data = new
            {
                suggest = new
                {
                    top = new
                    {
                        topResult = (object)null,
                        movies = System.Array.Empty<object>(),
                    },
                },
            },
        };

        httpFactory.SetResponse(new() { Content = JsonContent.Create(data) });

        await foreach (var _ in graphql.SuggestSearch<FilmInfo>("keyword", token)) { }

        var body = httpFactory.LastRequestBody;
        Assert.NotNull(body);

        var request = JsonDocument.Parse(body).RootElement;
        var variables = request.GetProperty("variables");

        Assert.True(variables.TryGetProperty("yandexCityId", out var cityId),
                    $"yandexCityId missing from variables: {variables}");
        Assert.Equal(JsonValueKind.Number, cityId.ValueKind);
    }
}
