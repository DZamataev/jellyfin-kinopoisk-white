using System.Collections.Generic;

using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Entities;

namespace KinopoiskWhite.Api.Models;

using Extensions;

public abstract record BaseMetadata
{
    public int Id { get; init; }

    public abstract string GetRootPath();
    public abstract string GetItemPath();
    public abstract RemoteSearchResult GetSearchResult();
    public abstract IEnumerable<(ImageType, string)> GetImages();

    // Release year of this candidate, used to disambiguate same-named results.
    // Null when the metadata type carries no year (persons, episodes).
    public virtual int? GetYear() => null;
}

public record FilmInfo: BaseMetadata
{
    public override string GetRootPath() => "movies";
    public override string GetItemPath() => "movie";
    public override int? GetYear() => ProductionYear ?? KpProductionYear ?? OttProductionYear;

    public string ContentId { get; init; } = "";
    public FilmTitle Title { get; init; }
    public FilmRating Rating { get; init; }
    public FilmGallery Gallery { get; init; }
    public FilmImages Images { get; init; }
    public FilmActors Actors { get; init; }
    public FilmCrewMembers Directors { get; init; }
    public FilmCrewMembers Writers { get; init; }
    public FilmCrewMembers Producers { get; init; }
    public FilmCrewMembers Operators { get; init; }
    public FilmCrewMembers Composers { get; init; }
    public FilmCrewMembers Designers { get; init; }
    public FilmCrewMembers FilmEditors { get; init; }
    public int? ProductionYear { get; init; }
    public int? KpProductionYear { get; init; }
    public int? OttProductionYear { get; init; }
    public string Tagline { get; init; } = "";
    public string ShortDescription { get; init; } = "";
    public string Synopsis { get; init; } = "";
    public Genre[] Genres { get; init; } = [];
    public FilmPremiere WorldPremiere { get; init; }
    public FilmRestriction Restriction { get; init; }

    public record Genre(string Name = "", string Slug = "");

    public record FilmRestriction
    {
        public string Age { get; init; } = "";
        public string Mpaa { get; init; } = "";

        public string Rating
        {
            get
            {
                if (!string.IsNullOrEmpty(Mpaa))
                    return Mpaa?.ToUpper();
                if (string.IsNullOrEmpty(Age))
                    return Age;
                return "Unrated";
            }
        }
    }

    public record FilmRating
    {
        public RatingValue Imdb { get; init; }
        public RatingValue Kinopoisk { get; init; }
        public RatingValue RussianCritics { get; init; }
        public RatingWithVotesValue WorldwideCritics { get; init; }
        public RatingValue ReviewCount { get; init; }

        public float? Community => Imdb?.Value ?? Kinopoisk?.Value;
        public float? Critics => WorldwideCritics?.Percent;

        public record RatingValue
        {
            public float? Value { get; init; }
            public bool? IsActive { get; init; }
            public int? Count { get; init; }
        }

        public record RatingWithVotesValue : RatingValue
        {
            public float? Percent { get; init; }
            public int? PositiveCount { get; init; }
            public int? NegativeCount { get; init; }
        }
    }
    
    public record FilmBoxOffice
    {
        public MoneyAmount Budget { get; init; }
        public MoneyAmount RusBox { get; init; }
        public MoneyAmount UsaBox { get; init; }
        public MoneyAmount WorldBox { get; init; }
        public MoneyAmount Marketing { get; init; }

        public record MoneyAmount
        {
            public int? Amount { get; init; } = 0;
            public FilmCurrency Currency { get; init; }
            public string AmountString => $"{Currency.Symbol}{Amount}";

            public record FilmCurrency(string Symbol = "");
        }
    }

    public record FilmPremiere
    {
        public FilmIncompleteDate IncompleteDate { get; init; }
    }

    public override RemoteSearchResult GetSearchResult()
    {
        string title;
        if (!string.IsNullOrWhiteSpace(Title?.Russian))
        {
            title = Title.Russian;
            if (!string.IsNullOrWhiteSpace(Title?.Original))
                title += $" ({Title.Original})";
        }
        else
            title = Title?.Original;

        RemoteSearchResult result = new()
        {
            Name = title,
            ProductionYear = ProductionYear,
            ImageUrl = (
                Gallery?.Posters?.HdVertical ??
                Gallery?.Posters?.KpVertical ??
                Gallery?.Posters?.Vertical ??
                Gallery?.Posters?.MarketingVertical
            )?.Medium,
        };
        result.SetDefaultId(Id);
        result.SetContentId(ContentId);
        return result;
    }

    public override IEnumerable<(ImageType, string)> GetImages()
    {
        Dictionary<FilmImageType, ImageType> mapper = new() {
            {FilmImageType.POSTER, ImageType.Primary},
            {FilmImageType.COVER, ImageType.Primary},
            {FilmImageType.FAN_ART, ImageType.Primary},
            {FilmImageType.WALLPAPER, ImageType.Backdrop},
            {FilmImageType.STILL, ImageType.Backdrop},
            {FilmImageType.SCREENSHOT, ImageType.Backdrop},
        };

        if (Gallery != null)
        {
            yield return (ImageType.Logo, Gallery.Logos?.Horizontal?.Url);
            yield return (ImageType.Primary, Gallery.Posters?.Vertical?.Url);
            yield return (ImageType.Primary, Gallery.Posters?.MarketingVertical?.Url);
        }

        var items = Images?.Items;
        if (items != null)
            foreach (var item in items)
                if (mapper.TryGetValue(item.Type, out ImageType type))
                    yield return (type, item.Image.Url);
    }
}

public record FilmIncompleteDate
{
    public string Accuracy { get; init; } = "";
    public string Date { get; init; } = "";
}
public record FilmTitle(string Russian = "", string Original = "");

public record FilmSeason(int? Id, int Number, string ContentId = "");
