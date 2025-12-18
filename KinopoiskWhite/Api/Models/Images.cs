using System.Text.Json.Serialization;

namespace KinopoiskWhite.Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FilmImageType
{
    POSTER,
    COVER,
    STILL,
    WALLPAPER,
    SCREENSHOT,
    FAN_ART,
    // PROMO,
    // CONCEPT
    // SHOOTING,
}

public record FilmImages
{
    public ListImage[] Items { get; init; }= [];
    public int Total { get; init; }

    public record ListImage
    {
        public int? Id { get; init; }
        public FilmImageType Type { get; init; }
        public Image Image { get; init; }
    }
}

public record Image
{
    public string AvatarsUrl { get; init; } = "";
    public ImageSize OrigSize { get; init; }

    public record ImageSize(int? Width, int? Height);

    protected int MaxDimension => 600;
    private string BaseUrl => $"https:{AvatarsUrl}";
    public string Small => $"{BaseUrl}/300x";
    public string Medium => $"{BaseUrl}/576x";
    public string Large => $"{BaseUrl}/3840x";
    public string Url => Medium;
}

public record FilmGallery
{
    public FilmGalleryImages Covers { get; init; }
    public FilmGalleryImages Logos { get; init; }
    public FilmGalleryImages Posters { get; init; }

    public record FilmGalleryImages(
        Image Square,
        Image Horizontal,
        Image Vertical,
        Image MarketingVertical,
        Image HdVertical,
        Image KpVertical
    );
}