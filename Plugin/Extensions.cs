using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.KinopoiskWhite
{
    public static class StringExtensions
    {
        public static ShortInfo GetShortInfo(this string jsonString) {
            using var doc = System.Text.Json.JsonDocument.Parse(jsonString);
            var global = doc.RootElement
                .GetProperty("data")
                .GetProperty("suggest")
                .GetProperty("top")
                .GetProperty("topResult")
                .GetProperty("global");

            var options = new System.Text.Json.JsonSerializerOptions {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            };

            var film = System.Text.Json.JsonSerializer.Deserialize<Film>(global, options);

            return new ShortInfo {
                Id = film.Id,
                Title = film.Title.Russian,
                TitleOrig = film.Title.Original,
                Rating = film.Rating.Kinopoisk.Value,
                Poster = film.Gallery.Posters.HdVertical.AvatarsUrl,
                ProductionYear = film.ProductionYear,
            };
        }

    }
}