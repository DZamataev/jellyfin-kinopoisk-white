using System.Collections.Generic;

namespace Plugin.Api;

public record FilmInfo
{
    public int Id { get; init; }
    public string ContentId { get; init; } = "";
    public FilmTitle Title { get; init; }
    public FilmRating Rating { get; init; }
    public FilmGallery Gallery { get; init; }
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
    public List<Genre> Genres { get; init; } = [];
    public FilmPremiere WorldPremiere { get; init; }
    public FilmRestriction Restriction { get; init; }

    public record FilmTitle(string Russian = "", string Original = "");
    public record Genre(string Name = "", string Slug = "");

    public string Kid => System.Convert.ToString(Id);

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

    public record FilmGallery
    {
        public FilmImages Covers { get; init; }
        public FilmImages Logos { get; init; }
        public FilmImages Posters { get; init; }

        public string Primary => Posters?.Vertical?.Url;
        public string Backdrop => Covers?.Horizontal?.Url;
        public string Logo => Logos?.Horizontal?.Url;

        public record FilmImages(
            Image Square,
            HorizontalImage Horizontal,
            VerticalImage Vertical,
            VerticalImage MarketingVertical,
            VerticalImage HdVertical,
            VerticalImage KpVertical
        );

        public record Image
        {
            public string AvatarsUrl { get; init; } = "";
            public ImageSize OrigSize { get; init; }

            public record ImageSize(int? Width, int? Height);

            private string BaseUrl => $"https:{AvatarsUrl}";
            protected virtual string DefaultSize => "100x100";
            public string Url
            {
                get
                {
                    var (width, height) = (OrigSize?.Width, OrigSize?.Height);
                    return (width != null && height != null)
                        ? $"{BaseUrl}/{width}x{height}"
                        : $"{BaseUrl}/{DefaultSize}";
                }
            }
        }
        public record VerticalImage: Image
        {
            protected override string DefaultSize => "600x900";
        }
        public record HorizontalImage: Image
        {
            protected override string DefaultSize => "900x600";
        }
    }

    public record FilmCrewMembers
    {
        public List<FilmCrewMember> Items { get; init; } = [];

        public record FilmCrewMember
        {
            public FilmPerson Person { get; init; }
            public record FilmPerson(int Id, string Name = "", string OriginalName = "");
        }
    }

    public record FilmActors : FilmCrewMembers
    {
        public int? Total { get; init; } = 0;
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

        public record FilmIncompleteDate
        {
            public string Accuracy { get; init; } = "";
            public string Date { get; init; } = "";
        }
    }
}