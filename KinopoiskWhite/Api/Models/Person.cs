using MediaBrowser.Model.Providers;

namespace KinopoiskWhite.Api.Models;

using System.Collections.Generic;
using Extensions;
using MediaBrowser.Model.Entities;

public record FilmPerson : BaseMetadata {
    public override string GetRootPath() => "persons";
    public override string GetItemPath() => "person";

    public string Name { get; init; } = "";
    public string OriginalName { get; init; } = "";
    public Gallery[] Gallery { get; init; }
    public FilmPersonImg Img { get; init; }

    public override RemoteSearchResult GetSearchResult()
    {
        RemoteSearchResult result = new() { Name = Name };
        result.SetDefaultId(Id);
        return result;
    }

    public override IEnumerable<(ImageType, string)> GetImages()
    {
        if (Img?.PosterMedium?.X2 != null)
            yield return (ImageType.Primary, $"https:{Img.PosterMedium.X2}");
        foreach (var img in Gallery ?? [])
        {
            if (img != null)
                yield return (ImageType.Primary, $"https:{img.BaseUrl}/576x");
        }
    }
}

public record FilmPersonImg(XImage PosterMedium, XImage Snippet, XImage Photo);
public record XImage(string X1, string X2);
public record Gallery(string BaseUrl, string Url);

public record FilmCrewMembers
{
    public FilmCrewMember[] Items { get; init; } = [];

    public record FilmCrewMember
    {
        public FilmPerson Person { get; init; }
    }
}

public record FilmActors : FilmCrewMembers
{
    public int? Total { get; init; } = 0;
}