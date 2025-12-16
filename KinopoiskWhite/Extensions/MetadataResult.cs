using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

namespace KinopoiskWhite.Extensions;

using Api.Models;

public static class MetadataResultExtensions
{
    public static void
    FillFrom(this BaseItem item, FilmInfo metadata)
    {
        item.SetDefaultId(metadata.Kid);
        item.SetContentId(metadata.ContentId);

        item.Name = metadata.Title.Russian;
        item.OriginalTitle = metadata.Title.Original;
        item.ProductionYear = metadata.ProductionYear;
        item.CommunityRating = metadata.Rating.Community;
        item.CriticRating = metadata.Rating.Critics;
        item.CustomRating = metadata.Restriction?.Rating;

        item.Tagline = metadata.ShortDescription;
        item.Overview = metadata.Synopsis;

        foreach (var genre in metadata.Genres)
            item.AddGenre(genre.Slug);
    }

    public static void
    FillFrom<T>(this MetadataResult<T> result, FilmInfo metadata) where T : BaseItem
    {
        result.Item.FillFrom(metadata);

        result.AddCrewMembers(PersonKind.Actor, metadata.Actors);
        result.AddCrewMembers(PersonKind.Director, metadata.Directors);
        result.AddCrewMembers(PersonKind.Writer, metadata.Writers);
        result.AddCrewMembers(PersonKind.Producer, metadata.Producers);
        result.AddCrewMembers(PersonKind.Composer, metadata.Composers);
        result.AddCrewMembers(PersonKind.Editor, metadata.FilmEditors);

        result.HasMetadata = true;
    }

    static void AddCrewMembers<T>(this MetadataResult<T> target, PersonKind type,
                                  FilmInfo.FilmCrewMembers members) where T : BaseItem
    {
        foreach (var member in members?.Items ?? [])
        {
            if (member?.Person?.Name == null) return;
            target.AddPerson(new PersonInfo { Name = member.Person.Name, Type = type });
        }
    }
}