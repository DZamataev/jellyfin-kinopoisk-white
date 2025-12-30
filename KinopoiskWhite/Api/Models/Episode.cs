using System.Collections.Generic;

using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Entities;

namespace KinopoiskWhite.Api.Models;
using Extensions;

public record FilmEpisode : BaseMetadata
{
    public int Number { get; init; }
    public FilmIncompleteDate ReleaseDate { get; init; }
    public FilmSeason Season { get; init; }
    public FilmTitle Title { get; init; }

    public string ContentId { get; init; } = "";
    public string Synopsis { get; init; } = "";
    public Image Cover { get; init; }

    public override IEnumerable<(ImageType, string)> GetImages() => [];
    public override string GetItemPath() => null;
    public override string GetRootPath() => null;

    public override RemoteSearchResult GetSearchResult()
    {
        RemoteSearchResult result = new() {
            Name = Title.Russian ?? Title.Original,
        };
        foreach (var (_, url) in GetImages())
        {
            result.ImageUrl = url;
            break;
        }

        result.SetDefaultId(Id);

        return result;
    }
}