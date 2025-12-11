using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.KinopoiskWhite;

public static class Extensions
{
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