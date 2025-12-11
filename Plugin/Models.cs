using System.Collections.Generic;
using Jellyfin.Data.Entities.Libraries;
using MediaBrowser.Controller.Entities;
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
    public FilmTitle Title { get; set; }
    public FilmRating Rating { get; set; }
    public FilmGallery Gallery { get; set; }
    public FilmActors Actors { get; set; }
    public FilmCrewMembers Directors { get; set; }
    public FilmCrewMembers Writers { get; set; }
    public FilmCrewMembers Producers { get; set; }
    public FilmCrewMembers Operators { get; set; }
    public FilmCrewMembers Composers { get; set; }
    public FilmCrewMembers Designers { get; set; }
    public FilmCrewMembers FilmEditors { get; set; }
    public int? ProductionYear { get; set; }
    public int? KpProductionYear { get; set; }
    public int? OttProductionYear { get; set; }
    public string Tagline { get; set; }
    public string ShortDescription { get; set; }
    public string Synopsis { get; set; }
    public List<Genre> Genres { get; set; } = [];
    public FilmPremiere WorldPremiere { get; set; }
    public FilmRestriction Restriction { get; set; }

    public void Fill(BaseItem target)
    {
        target.SetProviderId(Constants.ProviderId, System.Convert.ToString(Id));
        target.Name = Title.Russian;
        target.OriginalTitle = Title.Original;
        target.ProductionYear = ProductionYear;
        target.CommunityRating = Rating.Community;
        target.CriticRating = Rating.Critics;
        target.CustomRating = Restriction?.Mpaa ?? "";

        target.Tagline = ShortDescription;
        target.Overview = Synopsis;

        foreach (var genre in Genres)
            target.AddGenre(genre.Slug);

        // foreach (var person in Actors.Items)
        //     movie.AddPerson();

        return;
    }

    public class FilmTitle
    {
        public string Russian { get; set; }
        public string Original { get; set; }
    }

    public class Genre
    {
        public string Name { get; set; }
        public string Slug { get; set; }
    }

    public class FilmRestriction
    {
        public string Age { get; set; }
        public string Mpaa { get; set; }
    }

    public class FilmRating
    {
        public RatingValue Imdb { get; set; }
        public RatingValue Kinopoisk { get; set; }
        public RatingValue RussianCritics { get; set; }
        public RatingWithVotesValue WorldwideCritics { get; set; }
        public RatingValue ReviewCount { get; set; }

        public float Community => System.Convert.ToSingle(Imdb?.Value ?? Kinopoisk?.Value ?? 0);
        public float Critics => System.Convert.ToSingle(
            WorldwideCritics?.Value ?? RussianCritics?.Value ?? 0
        );
        public string Custom => System.Convert.ToString(ReviewCount?.Count);

        public class RatingValue
        {
            public double? Value { get; set; }
            public bool? IsActive { get; set; }
            public int? Count { get; set; }
        }

        public class RatingWithVotesValue : RatingValue
        {
            public double? Percent { get; set; }
            public int? PositiveCount { get; set; }
            public int? NegativeCount { get; set; }
        }
    }

    public class FilmGallery
    {
        public FilmPosters Posters { get; set; }

        public class FilmCovers
        {
            public Image Square { get; set; }
            public Image Horizontal { get; set; }
        }
        public class FilmPosters
        {
            public Image MarketingVertical { get; set; }
            public Image HdVertical { get; set; }
            public Image KpVertical { get; set; }
        }
        public class Image
        {
            public string AvatarsUrl { get; set; }

            public string Url => $"https://{AvatarsUrl}";
            public string Tiny => $"{Url}/100x100";
            public string Small => $"{Url}/100x100";
            public string Medium => $"{Url}/400x400";
            public string Large => $"{Url}/800x800";
            public string ExtraLarge => $"{Url}/1200x1200";
        }
    }

    public class FilmCrewMembers
    {
        public List<FilmCrewMember> Items { get; set; } = [];

        public class FilmCrewMember
        {
            public FilmPerson Person { get; set; }

            public class FilmPerson
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public string OriginalName { get; set; }
            }
        }
    }

    public class FilmActors : FilmCrewMembers
    {
        public int Total { get; set; } = 0;
    }

    public class FilmBoxOffice
    {
        public MoneyAmount Budget { get; set; }
        public MoneyAmount RusBox { get; set; }
        public MoneyAmount UsaBox { get; set; }
        public MoneyAmount WorldBox { get; set; }
        public MoneyAmount Marketing { get; set; }

        public class MoneyAmount
        {
            public int Amount { get; set; } = 0;
            public FilmCurrency Currency { get; set; }

            public string AmountString => $"{Currency.Symbol}{Amount}";

            public class FilmCurrency
            {
                public string Symbol { get; set; }
            }
        }
    }

    public class FilmPremiere
    {
        public FilmIncompleteDate IncompleteDate { get; set; }

        public class FilmIncompleteDate
        {
            public string Accuracy { get; set; }
            public string Date { get; set; }
        }
    }
}