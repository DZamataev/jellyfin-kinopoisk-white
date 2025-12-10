using System.Collections.Generic;

namespace Jellyfin.Plugin.KinopoiskWhite
{
    public class ShortInfo
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string TitleOrig { get; set; }
        public double Rating { get; set; }
        public string Poster { get; set; }
        public int ProductionYear { get; set; }
    }

    public record Film
    {
        public int Id { get; set; }
        public string ContentId { get; set; }
        public Title Title { get; set; }
        public Rating Rating { get; set; }
        public MovieGallery Gallery { get; set; }
        public int ProductionYear { get; set; }
    }

    public class Title
    {
        public string Russian { get; set; }
        public string Original { get; set; }
    }

    public class Rating
    {
        public RatingValue Kinopoisk { get; set; }
    }

    public class RatingValue
    {
        public bool IsActive { get; set; }
        public double Value { get; set; }
    }

    public class MovieGallery
    {
        public MoviePosters Posters { get; set; }
    }

    public class MoviePosters
    {
        public Image HdVertical { get; set; }
        public Image KpVertical { get; set; }
    }

    public class Image
    {
        public string AvatarsUrl { get; set; }
        public object FallbackUrl { get; set; }
    }
}