using Jellyfin.Data.Entities.Libraries;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.KinopoiskWhite;

public class KinopoiskExternalId : IExternalId
{
    public string ProviderName => Constants.ProviderName;
    public string Key => Constants.ProviderId;
    public string UrlFormatString => "https://www.kinopoisk.ru/film/{0}";
    public ExternalIdMediaType? Type => null;
    public bool Supports(IHasProviderIds item)
    {
        return item is Movie || item is Series;
    }
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
    public RatingValue Imdb { get; set; }
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