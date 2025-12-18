namespace KinopoiskWhite.Api.Models;

public record FilmInfo
{
    public bool QueriedById { get; init; } = false;
    public int Id { get; init; }
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

    public record FilmTitle(string Russian = "", string Original = "");
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

        public record FilmIncompleteDate
        {
            public string Accuracy { get; init; } = "";
            public string Date { get; init; } = "";
        }
    }
}