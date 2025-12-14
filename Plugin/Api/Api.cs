using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

namespace Plugin.Api;
using Common;

public class KinopoiskApi
{
    private readonly GraphQL _graphql;

    public KinopoiskApi(GraphQL graphql, ILogger<KinopoiskApi> logger, IHttpClientFactory httpClientFactory)
    {
        _graphql = graphql;
    }

    public async Task<string> GetKinopoiskId(ItemLookupInfo info, CancellationToken cancellationToken)
    {
        var keywords = info.Path.ParseFileName();

        foreach (var (title, year) in keywords)
        {
            string keyword = (year == null) ? title : $"{title} {year}";

            var film = await _graphql
                .SuggestSearch(keyword, cancellationToken)
                .ConfigureAwait(false);

            if (film?.Id != null)
                return System.Convert.ToString(film.Id);
        }
        throw new System.Exception($"Get Kinopoisk Id failed [{info.Name}].\n{keywords}");
    }

    public async Task Fetch<T>(MetadataResult<T> itemResult, string kinopoiskId, string language, string country, CancellationToken cancellationToken)
            where T : BaseItem
    {
        if (string.IsNullOrWhiteSpace(kinopoiskId))
        {
            throw new System.ArgumentNullException(nameof(kinopoiskId));
        }

        var kid = System.Convert.ToInt32(kinopoiskId);
        var film = await _graphql
            .FilmBaseInfo(kid, cancellationToken)
            .ConfigureAwait(false);
        
        if (film != null)
        {
            Fill(film, itemResult);
            return;
        }

        throw new System.Exception(
            $"Get Kinopoisk metadata failed KID {kinopoiskId} [{itemResult.Item.Name}].");
    }

    private static void Fill<T>(FilmInfo film, MetadataResult<T> target) where T : BaseItem
    {
        target.Item.SetProviderId(Constants.ProviderId, System.Convert.ToString(film.Id));
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

        void AddPerson(PersonKind Type, FilmInfo.FilmCrewMembers.FilmCrewMember Crew)
        {
            if (Crew?.Person?.Name == null) return;
            target.AddPerson(new PersonInfo { Name = Crew.Person.Name, Type = Type });
        }

        foreach (var person in film.Actors.Items) AddPerson(PersonKind.Actor, person);
        foreach (var person in film.Directors.Items) AddPerson(PersonKind.Director, person);
        foreach (var person in film.Writers.Items) AddPerson(PersonKind.Writer, person);
        foreach (var person in film.Producers.Items) AddPerson(PersonKind.Producer, person);
        foreach (var person in film.Composers.Items) AddPerson(PersonKind.Composer, person);
        foreach (var person in film.FilmEditors.Items) AddPerson(PersonKind.Editor, person);

        return;
    }

}