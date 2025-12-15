using System.Linq;
using System.Collections.Generic;

using Jellyfin.Data.Enums;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

namespace Plugin.Extensions;

using Api;
using Common;

public static class FilmInfoExtensions
{
    public static void
    Fill<T>(this FilmInfo film, MetadataResult<T> target) where T : BaseItem
    {
        target.Item.SetProviderId(Constants.ProviderId, film.Kid);

        if (!string.IsNullOrEmpty(film.ContentId))
            target.Item.SetProviderId(Constants.ProviderName, film.ContentId);

        target.Item.Name = film.Title.Russian;
        target.Item.OriginalTitle = film.Title.Original;
        target.Item.ProductionYear = film.ProductionYear;
        target.Item.CommunityRating = film.Rating.Community;
        target.Item.CriticRating = film.Rating.Critics;
        target.Item.CustomRating = film.Restriction?.Rating;

        target.Item.Tagline = film.ShortDescription;
        target.Item.Overview = film.Synopsis;

        foreach (var genre in film.Genres)
            target.Item.AddGenre(genre.Slug);

        void AddCrew(PersonKind Type, FilmInfo.FilmCrewMembers members)
        {
            foreach (var crew in members?.Items ?? [])
            {
                if (crew?.Person?.Name == null) return;
                target.AddPerson(new PersonInfo { Name = crew.Person.Name, Type = Type });
            }
        }

        AddCrew(PersonKind.Actor, film.Actors);
        AddCrew(PersonKind.Director, film.Directors);
        AddCrew(PersonKind.Writer, film.Writers);
        AddCrew(PersonKind.Producer, film.Producers);
        AddCrew(PersonKind.Composer, film.Composers);
        AddCrew(PersonKind.Editor, film.FilmEditors);

        target.HasMetadata = true;

        return;
    }

    public static IEnumerable<RemoteImageInfo> FillImages(this FilmInfo film)
    {
        var res = Enumerable.Empty<RemoteImageInfo>();

        static RemoteImageInfo fill(ImageType type, string image)
        {
            if (image == null) return null;

            return new RemoteImageInfo
            {
                Type = type,
                Url = image,
                Language = Constants.ProviderMetadataLanguage,
                ProviderName = Constants.ProviderName,
            };
        }

        (ImageType, string)[] images = [
            (ImageType.Primary, film.Gallery?.Primary),
            // (ImageType.Backdrop, film.Gallery?.Backdrop),
            // (ImageType.Logo, film.Gallery?.Logo),
        ];

        foreach (var (type, url) in images)
        {
            var result = fill(type, url);

            if (result != null)
                yield return result;
        }
    }
}