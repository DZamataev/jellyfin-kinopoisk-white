using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.KinopoiskWhite
{
    public static class MovieExtensions
    {
        public static int GetShortInfo(this Movie movie, string jsonString) {
            using var doc = JsonDocument.Parse(jsonString);

            if (doc.RootElement.ValueKind == JsonValueKind.Null)
                throw new System.Exception("Document is null");

            var root = doc.RootElement
                .GetProperty("data")
                .GetProperty("suggest")
                .GetProperty("top")
                .GetProperty("topResult")
                .GetProperty("global");

            var options = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            var film = JsonSerializer.Deserialize<Film>(root, options);

            movie.SetProviderId(Constants.ProviderId, System.Convert.ToString(film.Id));
            movie.Name = film.Title.Russian;
            movie.OriginalTitle = film.Title.Original;
            movie.ProductionYear = film.ProductionYear;
            movie.CommunityRating = System.Convert.ToSingle(film.Rating.Kinopoisk.Value);
            // Poster = film.Gallery.Posters.HdVertical.AvatarsUrl;

            return film.Id;
        }

        public static void GetFullInfo(this Movie movie, string jsonString) {
            using var doc = JsonDocument.Parse(jsonString);

            if (doc.RootElement.ValueKind == JsonValueKind.Null)
                throw new System.Exception("Document is null");

            var root = doc.RootElement
                .GetProperty("data")
                .GetProperty("film");

            var options = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            var film = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(root, options);

            var kid = film["id"].GetInt32();
            movie.SetProviderId(Constants.ProviderId, System.Convert.ToString(kid));
            movie.Tagline = film["shortDescription"].GetString();
            movie.Overview = film["synopsis"].GetString();

            movie.Name = film["title"].GetProperty("russian").GetString();
            movie.OriginalTitle = film["title"].GetProperty("original").GetString();
            movie.ProductionYear = film["productionYear"].GetInt32();
            movie.CommunityRating = film["rating"]
                .GetProperty("imdb")
                .GetProperty("value")
                .GetSingle();

            var genres = film["genres"]
                .EnumerateArray()
                .Select(item => item.GetProperty("slug").GetString())
                .ToList();

            foreach (var genre in genres)
                movie.AddGenre(genre);
        }
    }
}