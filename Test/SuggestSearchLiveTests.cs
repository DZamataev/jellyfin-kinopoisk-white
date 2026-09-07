using Xunit;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

using KinopoiskWhite.Api;
using KinopoiskWhite.Api.Models;

namespace Test;

// Opt-in live check against the real KinoPoisk API, driving the plugin's own
// GraphQL.SuggestSearch — the code path that produced 91 `[ERR] Error in
// "КиноПоиск (белый список)"` entries on 2026-09-07.
//
// This is the only test that proves the yandexCityId fix: the unit tests use mocked
// HTTP and cannot observe the server-side fault. Requires network and KinoPoisk's
// allow-listed query, so it runs only with KPW_KINOPOISK=1, like ParseResultsCsvTests.
public class SuggestSearchLiveTests
{
    static bool Enabled =>
        (Environment.GetEnvironmentVariable("KPW_KINOPOISK") ?? "") is "1" or "true" or "yes";

    sealed class RealHttpClientFactory : IHttpClientFactory
    {
        readonly HttpClient _client = new() { Timeout = TimeSpan.FromSeconds(20) };

        public HttpClient CreateClient(string name)
        {
            if (!_client.DefaultRequestHeaders.Contains("service-id"))
                _client.DefaultRequestHeaders.Add("service-id", "25");
            return _client;
        }
    }

    [Fact]
    public async Task SuggestSearchShouldResolveRealTitles()
    {
        if (!Enabled) return; // opt-in; see class comment

        var logger = LoggerFactory.Create(builder => builder.AddDebug());
        var graphql = new GraphQL(logger, new RealHttpClientFactory());
        var token = CancellationToken.None;

        // Titles from the actual library that failed during the outage.
        string[] keywords = [
            "Ирония судьбы",
            "Полиция Токио",
            "Иван Васильевич меняет профессию",
            "Ёжик в тумане",
            "Приключения Электроника",
        ];

        var failures = new List<string>();

        foreach (var keyword in keywords)
        {
            try
            {
                var found = false;
                await foreach (var film in graphql.SuggestSearch<FilmInfo>(keyword, token))
                    if (film != null && film.Id != 0) { found = true; break; }

                if (!found) failures.Add($"{keyword}: no result");
            }
            catch (Exception e)
            {
                failures.Add($"{keyword}: {e.GetType().Name} {e.Message}");
            }

            await Task.Delay(300); // be polite to the API
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }
}
