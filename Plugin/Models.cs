using System.Collections.Generic;

namespace Jellyfin.Plugin.KinopoiskWhite {
    public class ShortInfo
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string TitleOrig { get; set; }
        public double Rating { get; set; }
        public string Poster { get; set; }
    }

    public class SuggestResult
    {
        public SuggestResultGlobal TopResult { get; set; }
        public List<SuggestResultMovie> Movies { get; set; }
        public List<SuggestResultPerson> Persons { get; set; }
        public List<object> Cinemas { get; set; }
        public List<object> MovieLists { get; set; }
    }

    public class SuggestResultGlobal
    {
        public Film Global { get; set; }
    }

    public class Film
    {
        public int Id { get; set; }
        public string ContentId { get; set; }
        public Title Title { get; set; }
        public Rating Rating { get; set; }
        public MovieGallery Gallery { get; set; }
        public ViewOption ViewOption { get; set; }
        public TicketOption TicketOption { get; set; }
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

    public class ViewOption
    {
        public object ButtonText { get; set; }
        public bool IsAvailableOnline { get; set; }
        public string PurchasabilityStatus { get; set; }
        public object ContentPackageToBuy { get; set; }
        public object SubscriptionBadge { get; set; }
        public object Type { get; set; }
        public object AvailabilityAnnounce { get; set; }
    }

    public class TicketOption
    {
        public bool Purchasable { get; set; }
        public ReleaseAnnounce ReleaseAnnounce { get; set; }
    }

    public class ReleaseAnnounce
    {
        public bool Available { get; set; }
        public object ReleaseDate { get; set; }
    }

    public class SuggestResultMovie
    {
        public object Movie { get; set; }
    }

    public class SuggestResultPerson
    {
        public Person Person { get; set; }
    }

    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string OriginalName { get; set; }
        public string BirthDate { get; set; }
        public Image Poster { get; set; }
    }
}