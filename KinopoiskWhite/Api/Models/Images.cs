namespace KinopoiskWhite.Api.Models;

public enum FilmImageType
{
    POSTER,
    COVER,
    STILL,
    WALLPAPER,
    SCREENSHOT,
    SHOOTING,
    FAN_ART,
    PROMO,
    CONCEPT
}

public record FilmImages
{
    public ListImage[] Items { get; init; }= [];
    public int Total { get; init; }

    public record ListImage
    {
        public int? Id { get; init; }
        public string Type { get; init; } // public FilmImageType Type { get; init; }
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
    public string Url
    {
        get
        {
            var width = OrigSize?.Width ?? MaxDimension;
            var height = OrigSize?.Height ?? MaxDimension;

            var multiplier = MaxDimension / System.Math.Max(width, height);

            if (multiplier < 1) {
                if (width > height)
                {
                    width = MaxDimension;
                    height *= multiplier;
                }
                else
                {
                    height = MaxDimension;
                    width *= multiplier;
                }
            }
            return $"{BaseUrl}/{width}x{height}";
        }
    }
}

public record FilmGallery
{
    public FilmGalleryImages Covers { get; init; }
    public FilmGalleryImages Logos { get; init; }
    public FilmGalleryImages Posters { get; init; }

    public string Primary => (Posters?.Vertical ?? Posters?.MarketingVertical)?.Url;
    public string Backdrop => (Covers?.Horizontal ?? Covers.Square)?.Url;
    public string Logo => Logos?.Horizontal?.Url;

    public record FilmGalleryImages(
        Image Square,
        Image Horizontal,
        Image Vertical,
        Image MarketingVertical,
        Image HdVertical,
        Image KpVertical
    );
}