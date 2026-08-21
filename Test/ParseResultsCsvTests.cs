using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

using KinopoiskWhite.Extensions;

namespace Test;

using Common;

// Generates Test/parse-results.csv from the real library filename corpus: for every
// name it records what ParseFileName produces, and (opt-in) what the live KinoPoisk
// suggest API returns for the cleaned title.
//
// The API columns require network + KinoPoisk's allow-listed query, so this test only
// writes the file when KPW_KINOPOISK=1. A normal `dotnet test` run is a no-op here, so
// CI stays offline and the committed snapshot is left untouched. Parsing correctness
// itself is covered deterministically by RealLibraryParseTests.
public class ParseResultsCsvTests
{
    static bool Enabled =>
        (Environment.GetEnvironmentVariable("KPW_KINOPOISK") ?? "") is "1" or "true" or "yes";

    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(12) };

    [Fact]
    public async Task GenerateParseResultsCsv()
    {
        if (!Enabled) return; // opt-in; see class comment

        var testDir = FindTestDir();
        var corpus = Path.Combine(testDir, "library-filenames.txt");
        Assert.True(File.Exists(corpus), $"corpus not found: {corpus}");

        var names = File.ReadLines(corpus).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        var query = MockGraphQL.MockGetEmbeddedQuery("SuggestSearch");

        string Q(string s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";

        var rows = new List<string>
        {
            string.Join(",", "filename", "parsed_year", "primary_title", "clean_title",
                             "num_candidates", "kinopoisk_title", "kinopoisk_year", "kinopoisk_id",
                             "all_candidates")
        };

        foreach (var name in names)
        {
            var c = name.ParseFileName();
            var year = c.Select(x => x.Item2).FirstOrDefault(y => y != null);
            var primary = c.Length > 0 ? c[0].Item1 : "";
            var clean = c.Length > 0 ? c.OrderBy(x => x.Item1.Length).First().Item1 : "";
            var all = string.Join(" | ", c.Select(x => x.Item2 == null ? x.Item1 : $"{x.Item1} ({x.Item2})"));

            var (kpTitle, kpYear, kpId) = await Suggest(query, clean);

            rows.Add(string.Join(",", Q(name), Q(year?.ToString() ?? ""), Q(primary), Q(clean),
                                 c.Length.ToString(), Q(kpTitle), Q(kpYear), Q(kpId), Q(all)));

            await Task.Delay(120); // be polite to the API
        }

        var outPath = Path.Combine(testDir, "parse-results.csv");
        File.WriteAllText(outPath, string.Join("\n", rows) + "\n", new UTF8Encoding(true));

        Assert.Equal(names.Count + 1, rows.Count);
        Assert.True(File.Exists(outPath));
    }

    // Query the live KinoPoisk suggest API for the top result of a keyword. Returns
    // ("","","") for a valid "no match", ("(unreachable)","","") only after retries fail.
    static async Task<(string title, string year, string id)> Suggest(string query, string keyword)
    {
        var payload = JsonSerializer.Serialize(new
        {
            operationName = "SuggestSearch",
            variables = new { keyword, limit = 5 },
            query
        });

        for (int attempt = 1; attempt <= 4; attempt++)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, "https://graphql.kinopoisk.ru/graphql")
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };
                req.Headers.Add("service-id", "25");

                using var resp = await Http.SendAsync(req);
                if (!resp.IsSuccessStatusCode) throw new HttpRequestException($"HTTP {(int)resp.StatusCode}");

                var body = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);

                // Mirror the plugin: prefer topResult.global, else the first movies[] hit.
                var top = Nav(doc.RootElement, "data", "suggest", "top");
                var global = Nav(top, "topResult", "global");
                if (global.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                {
                    var movies = Nav(top, "movies");
                    global = movies.ValueKind == JsonValueKind.Array && movies.GetArrayLength() > 0
                             && movies[0].TryGetProperty("movie", out var m)
                        ? m : default;
                }
                if (global.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                    return ("", "", "");

                string title = "";
                if (global.TryGetProperty("title", out var t))
                    title = Str(t, "russian") is { Length: > 0 } r ? r : Str(t, "original");

                string year = "";
                if (global.TryGetProperty("productionYear", out var py) && py.ValueKind == JsonValueKind.Number)
                    year = py.GetInt32().ToString();
                else if (global.TryGetProperty("releaseYears", out var ry) && ry.ValueKind == JsonValueKind.Array
                         && ry.GetArrayLength() > 0 && ry[0].TryGetProperty("start", out var st)
                         && st.ValueKind == JsonValueKind.Number)
                    year = st.GetInt32().ToString();

                string id = global.TryGetProperty("id", out var idEl) ? idEl.ToString() : "";
                return (title, year, id);
            }
            catch when (attempt < 4)
            {
                await Task.Delay(400 * attempt); // backoff, then retry
            }
            catch
            {
                return ("(unreachable)", "", "");
            }
        }
        return ("(unreachable)", "", "");
    }

    static JsonElement Nav(JsonElement e, params string[] path)
    {
        foreach (var p in path)
        {
            if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(p, out var next))
                return default; // Undefined
            e = next;
        }
        return e;
    }

    static string Str(JsonElement e, string prop)
        => e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : "";

    static string FindTestDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "KinopoiskWhite"))
                && Directory.Exists(Path.Combine(dir.FullName, "Test")))
                return Path.Combine(dir.FullName, "Test");
            dir = dir.Parent;
        }
        return AppContext.BaseDirectory; // fallback
    }
}
