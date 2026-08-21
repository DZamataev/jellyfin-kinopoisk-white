using Xunit;

using KinopoiskWhite.Api.Models;

namespace Test;

// Regression tests for the NullReferenceException that occurred when the
// Kinopoisk suggest API returned an entry deserialized into a metadata record
// with a null Title (e.g. a non-Film suggestion type). GetSearchResult() must
// not throw in that case.
public class NullTitleRegressionTests
{
    [Fact]
    public void FilmInfo_GetSearchResult_NullTitle_DoesNotThrow()
    {
        var film = new FilmInfo { Id = 42 };            // Title left null
        var ex = Record.Exception(() => film.GetSearchResult());
        Assert.Null(ex);
    }

    [Fact]
    public void FilmInfo_GetSearchResult_OriginalOnly_UsesOriginal()
    {
        var film = new FilmInfo { Id = 42, Title = new FilmTitle(Russian: null, Original: "The Flight of Dragons") };
        var result = film.GetSearchResult();
        Assert.Equal("The Flight of Dragons", result.Name);
    }

    [Fact]
    public void FilmEpisode_GetSearchResult_NullTitle_DoesNotThrow()
    {
        var ep = new FilmEpisode { Id = 7 };            // Title left null
        var ex = Record.Exception(() => ep.GetSearchResult());
        Assert.Null(ex);
    }
}
