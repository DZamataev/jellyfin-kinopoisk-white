using Xunit;
using MediaBrowser.Controller.Entities.Movies;
using System.Linq;

namespace Jellyfin.Plugin.KinopoiskWhite.Tests; 


public class TransliterationTests {
    [Theory(Skip = "disabled")]
    [InlineData("fight club", /* 361, */ "Бойцовский клуб")]
    public async void ShouldGetShortInfo(string keyword, /* int exId, */ string exTitle) {
        var movie = new Movie();
        await Api.Instance.SuggestSearch(movie, keyword);
        // Assert.Equal(info.Id, exId);
        Assert.Equal(movie.Name, exTitle);
    }

    [Theory(Skip = "disabled")]
    [InlineData("/tmp/videos/Fight.Club.1999.1080p.BrRip.x264.YIFY.mp4", /* 361, */ "Бойцовский клуб")]
    public async void ShouldGetShortInfoFromPath(string path, /* int exId, */ string exTitle) {
        var (title, year) = Api.Instance.ParseFileName(path);
        var keyword = $"{title} {year}";
        var movie = new Movie();
        await Api.Instance.SuggestSearch(movie, keyword);
        // Assert.Equal(info.Id, exId);
        Assert.Equal(movie.Name, exTitle);
    }

    [Theory]
    [InlineData("Жить_2010.avi", "Жить", 2010)]
    [InlineData("Зелёный слоник.avi", "Зелёный слоник", null)]
    [InlineData("Хозяин_2025_WEB-DLRip-AVC.mkv", "Хозяин", 2025)]
    [InlineData("Девушка в тумане (2017) BDRip-AVC_ivanes20031987.mkv", "Девушка в тумане", 2017)]
    [InlineData("Three.Billboards.Outside.Ebbing,.Missouri.2017.720p.BluRay.x264-[YTS.AM].mp4", "Three Billboards Outside Ebbing Missouri", 2017)]
    [InlineData("28.Weeks.Later.2007.720p.BrRip.264.YIFY.mp4", "28 Weeks Later", 2007, 0)]
    [InlineData("28.Days.Later.2002.720p.BrRip.264.YIFY.mp4", "28 Days Later", 2002, 0)]
    [InlineData("28.Years.Later.2025.Proper.1080p.WEB-DL.DDP5.1.x265-NeoNoir.mkv", "28 Years Later", 2025, 0)]
    [InlineData("Kill.Bill.Vol.1.2003.1080p.BrRIp.x264.YIFY.mp4", "Kill Bill Vol 1", 2003, 0)]
    [InlineData("Mickey.17.2025.720p.WEBRip.x264.AAC-[YTS.MX].mp4", "Mickey 17", 2025, 0)]
    [InlineData("Nobody.2.2025.DUB.WEB-DLRip-AVC.seleZen.mkv", "Nobody 2", 2025, 0)]
    [InlineData("Terminator.2.1991.1080p.BluRay.x264-[YTS.AG].mp4", "Terminator 2", 1991, 0)]
    [InlineData("Сайлент Хилл 2 (2012) BDRip-AVC [Open Matte].mkv", "Сайлент Хилл 2", 2012, 0)]
    [InlineData("Brat_2_2000_WEB-DLRip_by_Dalemake.avi", "Brat 2", 2000, 0)]
    [InlineData("2001.A.Space.Odyssey.1968.1080p.BluRay.x264-[YTS.AM].mp4", "2001 A Space Odyssey", 1968, 0)]
    // position = 1
    [InlineData("Бумер Фильм второй_745.avi", "Бумер Фильм второй", null, 1)]
    [InlineData("Blade.Runner.2054.mp4", "Blade Runner 2054", null, 1)]
    [InlineData("04.Сумерки. Сага. Рассвет - Часть 1 (2011) BDRip 1080p [HEVC] 10 bit.mkv", "Сумерки Сага Рассвет Часть 1", 2011, 1)]
    // position = 2
    // [InlineData("Walk.the.Line.EXTENDED.2005.1080p.BrRip.x264.YIFY.mp4", "Walk the Line", 2005)]
    // [InlineData("Rock.n.Rolla.brrip.mkv", "Rock n Rolla", null)]
    public void ShouldGetListOfTitles(string path, string exTitle, int? exYear, int exPosition = 0)
    {
        var list = path.ParseFileName();

        foreach (var item in list.Select((item, index) => (item, index)).ToList())
        {
            var ((title, year), position) = item;
            if (title == exTitle && year == exYear) {
                Assert.Equal(exPosition, position);
                return;
            }
            System.Diagnostics.Debug.WriteLine($"{title}");
        };

        Assert.Fail($"Not found expected value {exTitle} ({exYear})");
    }
}
