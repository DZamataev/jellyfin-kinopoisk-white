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
    public string Small => $"{BaseUrl}/300x";
    public string Medium => $"{BaseUrl}/576x";
    public string Large => $"{BaseUrl}/3840x";
    public string Url => Medium;
    public string Calculated
    {
        get
        {
            int width = OrigSize?.Width ?? MaxDimension;
            int height = OrigSize?.Height ?? MaxDimension;

            float multiplier = MaxDimension / (float)System.Math.Max(width, height);

            if (multiplier < 1) {
                if (width > height)
                {
                    width = MaxDimension;
                    height = (int)(System.Convert.ToSingle(height) * multiplier);
                }
                else
                {
                    height = MaxDimension;
                    width = (int)(System.Convert.ToSingle(width) * multiplier);
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