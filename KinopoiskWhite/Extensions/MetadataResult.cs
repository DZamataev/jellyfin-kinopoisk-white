using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

namespace KinopoiskWhite.Extensions;

using Api.Models;

public static class MetadataResultExtensions
{
    public static void
    FillFrom<T>(this MetadataResult<T> result, BaseMetadata metadata) where T : BaseItem
    {
        switch (metadata)
        {
            case FilmInfo film:
                result.FillFrom(film);
                break;
            case FilmPerson person:
                result.FillFrom(person);
                break;
            case FilmEpisode episode:
                result.FillFrom(episode);
                break;
        };
    }

    private static void
    FillFrom(this BaseItem item, FilmInfo metadata)
    {
        item.SetDefaultId(metadata.Id);
        item.SetContentId(metadata.ContentId);

        item.Name = metadata.Title?.Russian;
        item.OriginalTitle = metadata.Title?.Original;
        item.ProductionYear = metadata.ProductionYear;
        item.CommunityRating = metadata.Rating?.Community;
        item.CriticRating = metadata.Rating?.Critics;
        item.CustomRating = metadata.Restriction?.Rating;

        item.Tagline = metadata.ShortDescription;
        item.Overview = metadata.Synopsis;

        foreach (var genre in metadata.Genres ?? [])
            item.AddGenre(genre.Slug);
    }

    private static void
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

    private static void AddCrewMembers<T>(this MetadataResult<T> target, PersonKind type,
                                  FilmCrewMembers members) where T : BaseItem
    {
        foreach (var member in members?.Items ?? [])
        {
            if (member?.Person?.Name == null) return;
            var person = new PersonInfo {
                Type = type,
                Name = member.Person.Name,
            };
            person.SetDefaultId(member.Person.Id);
            target.AddPerson(person);

        }
    }

    private static void
    FillFrom<T>(this MetadataResult<T> result, FilmPerson metadata) where T : BaseItem
    {
        result.Item.SetDefaultId(metadata.Id);
        result.Item.Name = metadata.Name;
        result.Item.OriginalTitle = metadata.OriginalName;
        // item.ProductionYear = metadata.BirthDate;
        result.HasMetadata = true;
    }

    private static void
    FillFrom<T>(this MetadataResult<T> result, FilmEpisode metadata) where T : BaseItem
    {
        result.Item.SetDefaultId(metadata.Id);
        result.Item.Name = metadata.Title?.Russian ?? metadata.Title?.Original;
        result.Item.OriginalTitle = metadata.Title?.Original;
        result.Item.ParentIndexNumber = metadata.Season?.Number;
        result.Item.IndexNumber = metadata.Number;
        result.Item.Overview = metadata.Synopsis;
        // item.ProductionYear = metadata.ReleaseDate;
        result.HasMetadata = true;
    }
}