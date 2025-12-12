using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities.Movies;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.KinopoiskWhite;

public static partial class Extensions
{

    [GeneratedRegex(@"[^\w]+|[_\s]")]
    private static partial Regex AllButWords();

    [GeneratedRegex(@"^\d+")]
    private static partial Regex LeadingDigits();

    [GeneratedRegex(@"\d+$")]
    private static partial Regex TrailingDigits();

    [GeneratedRegex(@"\.\w+$")]
    private static partial Regex FileExtension();

    [GeneratedRegex(@"((?:19|20)\d{2})")]
    private static partial Regex ByYear();

    public static (string, int?)[] ParseFileName(this string path)
    {
        var fileName = System.IO.Path.GetFileName(path);
        var byYear = ByYear();
        var parts = byYear.Split(fileName);

        var result = new HashSet<(string, int?)>();

        void push(string title, int? year)
        {
            title = AllButWords().Replace(title, " ").Trim();
            result.Add((title, year));

            title = LeadingDigits().Replace(title, "").Trim();
            result.Add((title, year));

            title = TrailingDigits().Replace(title, "").Trim();
            result.Add((title, year));
        }

        for (int index = parts.Length - 1; index >= 0; index--)
        {
            if (!Regex.IsMatch(parts[index], $"^{byYear.ToString()}$")) continue;

            var title = string.Join(" ", parts.Take(index));
            int year = int.Parse(parts[index]);
            push(title, year);
        }

        var fullName = FileExtension().Replace(fileName, "");
        push(fullName, null);

        return [.. result.OrderBy(x => x.Item1.Length)];
    }

    public static int GetShortInfo(this Movie movie, string jsonString)
    {
        using var doc = JsonDocument.Parse(jsonString);

        if (doc.RootElement.ValueKind == JsonValueKind.Null)
            throw new System.Exception("Document is null");

        var root = doc.RootElement
            .GetProperty("data")
            .GetProperty("suggest")
            .GetProperty("top")
            .GetProperty("topResult")
            .GetProperty("global");

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        var film = JsonSerializer.Deserialize<Film>(root, options);
        film.Fill(movie);
        return film.Id;
    }

    public static void GetFullInfo(this Movie movie, string jsonString)
    {
        using var doc = JsonDocument.Parse(jsonString);

        if (doc.RootElement.ValueKind == JsonValueKind.Null)
            throw new System.Exception("Document is null");

        var root = doc.RootElement
            .GetProperty("data")
            .GetProperty("film");

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        JsonSerializer.Deserialize<Film>(root, options).Fill(movie);
    }
}