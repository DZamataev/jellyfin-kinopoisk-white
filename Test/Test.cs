using Xunit;
using MediaBrowser.Controller.Entities.Movies;

namespace Jellyfin.Plugin.KinopoiskWhite.Tests; 


public class TransliterationTests {
    [InlineData("Дурак_2014.avi", "Дурак", 2014)]
    [InlineData("Майор_2013_BDRip_1,45.avi", "Майор", 2013)]
    [InlineData("Завод_2018_WEB-DLRip.avi", "Завод", 2018)]
    [InlineData("Сторож_2019.mkv", "Сторож", 2019)]
    [InlineData("Жить_2010.avi", "Жить", 2010)]
    [InlineData("Хозяин_2025_WEB-DLRip-AVC.mkv", "Хозяин", 2025)]
    [InlineData("The.Assassination.Of.Jesse.James.By.The.Coward.Robert.Ford.2007.1080p.BluRay.x264-[YTS.AM].mp4", "The Assassination Of Jesse James By The Coward Robert Ford", 2007)]
    [InlineData("The.Big.Short.2015.1080p.BluRay.x264-[YTS.AG].mp4", "The Big Short", 2015)]
    [InlineData("The.Great.Gatsby.2013.1080p.BluRay.x264.YIFY.mp4", "The Great Gatsby", 2013)]
    [InlineData("The.Lone.Ranger.2013.720p.BluRay.x264.YIFY.mp4", "The Lone Ranger", 2013)]
    [InlineData("There.Will.Be.Blood.2007.1080p.BluRay.x264-[YTS.AM].mp4", "There Will Be Blood", 2007)]
    [InlineData("Superbad_2007_Unrated_1080p_BluRay_Multi_Audio_HEVC_H265_BONE.mp4", "Superbad", 2007)]
    [InlineData("Mickey.17.2025.720p.WEBRip.x264.AAC-[YTS.MX].mp4", "Mickey 17", 2025)]
    [InlineData("The.Monkey.2025.720p.WEBRip.x264.AAC-[YTS.MX].mp4", "The Monkey", 2025)]
    [InlineData("Skull.2022.720p.WEBRip.x264.AAC-[YTS.MX].mp4", "Skull", 2022)]
    [InlineData("The.Northman.2022.720p.WEBRip.x264.AAC-[YTS.MX].mp4", "The Northman", 2022)]
    [InlineData("Onward.2020.720p.WEBRip.x264.AAC-[YTS.MX].mp4", "Onward", 2020)]
    [InlineData("Nobody.2.2025.DUB.WEB-DLRip-AVC.seleZen.mkv", "Nobody 2", 2025)]
    [InlineData("Dolgaya.progulka.2025.AMZN.WEB-DLRip.AVC.mkv", "Dolgaya progulka", 2025)]
    [InlineData("Bone.Lake.2024.1080p.WEBRip.x264.AAC5.1-[YTS.MX].mp4", "Bone Lake", 2024)]
    [InlineData("Drakula.2025.WEB-DLRip.avi", "Drakula", 2025)]
    [InlineData("Helloween.2025.1080p.BluRay.x264.AAC5.1-[YTS.MX].mp4", "Helloween", 2025)]
    [InlineData("Gadkaya.sestra.2025.AMZN.WEB-DLRip.AVC.mkv", "Gadkaya sestra", 2025)]
    [InlineData("Svodish.s.uma_2025_WEB-DL_1080p.mkv", "Svodish s uma", 2025)]
    [InlineData("Sailent.Hill.2006.DUAL.BDRip-AVC.AC3.-HQ-ViDEO.mkv", "Sailent Hill", 2006)]
    [InlineData("Other.2025.DUB.WEB-DLRip-AVC.x264.seleZen.mkv", "Other", 2025)]
    [InlineData("Сайлент Хилл 2 (2012) BDRip-AVC [Open Matte].mkv", "Сайлент Хилл 2", 2012)]
    [InlineData("After.Life.1998.HDRip_[1.46].avi", "After Life", 1998)]
    [InlineData("Кончится лето_2024_WEBRip.avi", "Кончится лето", 2024)]
    [InlineData("Frankenstein.2025.1080p.WEBRip.x265.10bit.AAC5.1-[YTS.MX].mp4", "Frankenstein", 2025)]
    [InlineData("One.Battle.After.Another.2025.1080p.WEBRip.x265.10bit.AAC5.1-[YTS.MX].mp4", "One Battle After Another", 2025)]
    [InlineData("Three.Billboards.Outside.Ebbing,.Missouri.2017.720p.BluRay.x264-[YTS.AM].mp4", "Three Billboards Outside Ebbing Missouri", 2017)]
    [InlineData("Mission.Impossible.-.The.Final.Reckoning.2025.HYBRID.IMAX.1080p.BluRay.x264.AAC5.1-[YTS.MX].mp4", "Mission Impossible The Final Reckoning", 2025)]
    [InlineData("The.Thirteenth.Floor.1999.1080p.BluRay.x264.YIFY.mp4", "The Thirteenth Floor", 1999)]
    [InlineData("Dead.End.2003.1080p.WEBRip.x264-[YTS.AM].mp4", "Dead End", 2003)]
    [InlineData("The.Straight.Story.1999.1080p.BluRay.x264-[YTS.AM].mp4", "The Straight Story", 1999)]
    [InlineData("Mulholland.Dr.2001.BluRay.720p.x264.YIFY.mp4", "Mulholland Dr", 2001)]
    [InlineData("Lost.Highway.1997.BrRip.720p.x264.YIFY.mkv", "Lost Highway", 1997)]
    [InlineData("Batman.The.Dark.Knight.2008.1080p.BluRay.x264.YIFY.mp4", "Batman The Dark Knight", 2008)]
    [InlineData("Interstellar.2014.1080p.BluRay.x264.YIFY.mp4", "Interstellar", 2014)]
    [InlineData("Batman.Begins.2005.1080p.BluRay.x264.YIFY.mp4", "Batman Begins", 2005)]
    [InlineData("The.Prestige.2006.1080p.BluRay.x264-[YTS.AM].mp4", "The Prestige", 2006)]
    [InlineData("The.Dark.Knight.Rises.2012.1080p.BluRay.x264.YIFY.mp4", "The Dark Knight Rises", 2012)]
    [InlineData("Dunkirk.2017.720p.BluRay.x264-[YTS.AG].mp4", "Dunkirk", 2017)]
    [InlineData("Ad.Astra.2019.1080p.BluRay.x264-[YTS.LT].mp4", "Ad Astra", 2019)]
    [InlineData("Song.of.the.sea.2014.MVO_xvid.avi", "Song of the sea", 2014)]
    [InlineData("Goodfellas.1990.1080p.BluRay.x264-[YTS.AM].mp4", "Goodfellas", 1990)]
    [InlineData("Ford.V.Ferrari.2019.1080p.BluRay.x264.AAC5.1-[YTS.LT].mp4", "Ford V Ferrari", 2019)]
    [InlineData("Downfall.2004.720p.BluRay.x264-[YTS.AM].mp4", "Downfall", 2004)]
    [InlineData("Filth.2013.1080p.BluRay.x264.YIFY.mp4", "Filth", 2013)]
    [InlineData("Zombieland.Double.Tap.2019.1080p.BluRay.x264-[YTS.LT].mp4", "Zombieland Double Tap", 2019)]
    [InlineData("Zombieland.2009.720p.BrRip.x264-YIFY.mp4", "Zombieland", 2009)]
    [InlineData("Top.Gun.Maverick.2022.1080p.BluRay.x264.AAC5.1-[YTS.MX].mp4", "Top Gun Maverick", 2022)]
    [InlineData("Top.Gun (1986) 1080p BrRip x264 1.29GB YIFY.mp4", "Top Gun", 1986)]
    [InlineData("Austin.Powers.Goldmember.2002.720p.Brrip.x264.Deceit.YIFY.mp4", "Austin Powers Goldmember", 2002)]
    [InlineData("Austin.Powers.International.Man.of.Mystery.1997.720p.Brrip.x264.Deceit.YIFY.mp4", "Austin Powers International Man of Mystery", 1997)]
    [InlineData("Austin.Powers.The.Spy.Who.Shagged.Me.1999.720p.Brrip.x264.Deceit.YIFY.mp4", "Austin Powers The Spy Who Shagged Me", 1999)]
    [InlineData("I,.Tonya.2017.720p.BluRay.x264-[YTS.AM].mp4", "I Tonya", 2017)]
    [InlineData("Idiocracy.2006.HDTV.720p.x264.YIFY.mp4", "Idiocracy", 2006)]
    [InlineData("Generation.P.2011.RUS.BDRip-AVC.AC3.-HQ-ViDEO.mkv", "Generation P", 2011)]
    [InlineData("Brat_1997_WEB-DLRip_by_Dalemake.avi", "Brat", 1997)]
    [InlineData("Brat_2_2000_WEB-DLRip_by_Dalemake.avi", "Brat 2", 2000)]
    [InlineData("Да и да 2014.avi", "Да и да", 2014)]
    [InlineData("Novaya.realnost.2022.WEB-DL.1080p.ELEKTRI4KA.UNIONGANG.mkv", "Novaya realnost", 2022)]
    [InlineData("Les.traducteurs.2019.BDRip.1.46Gb.MegaPeer.avi", "Les traducteurs", 2019)]
    [InlineData("Lawless.2012.BluRay.1080p.x264.YIFY.mp4", "Lawless", 2012)]
    [InlineData("Moneyball.2011.720p.BrRip.x264.YIFY.mp4", "Moneyball", 2011)]
    [InlineData("My.Own.Private.Idaho.1991.1080p.BluRay.x264.YIFY.mp4", "My Own Private Idaho", 1991)]
    [InlineData("Nightcrawler.2014.1080p.BluRay.x264.YIFY.mp4", "Nightcrawler", 2014)]
    [InlineData("Parasite.2019.1080p.BluRay.x264-[YTS.LT].mp4", "Parasite", 2019)]
    [InlineData("Rush.2013.1080p.BluRay.x264.YIFY.mp4", "Rush", 2013)]
    [InlineData("Saving.Private.Ryan.1998.1080p.BrRip.x264.YIFY.mp4", "Saving Private Ryan", 1998)]
    [InlineData("The.Departed.2006.BluRay.1080p.x264.YIFY.mp4", "The Departed", 2006)]
    [InlineData("The.Killing.Of.A.Sacred.Deer.2017.1080p.BluRay.x264-[YTS.AG].mp4", "The Killing Of A Sacred Deer", 2017)]
    [InlineData("The.Lion.King.2019.1080p.BluRay.x264-[YTS.LT].mp4", "The Lion King", 2019)]
    [InlineData("The.Ritual.2017.1080p.BluRay.x264-[YTS.AG].mp4", "The Ritual", 2017)]
    [InlineData("The.Tree.Of.Life.2011.1080p.BluRay.x264-[YTS.AG].mp4", "The Tree Of Life", 2011)]
    [InlineData("The.Truman.Show.1998.720p.x264.AAC.mkv", "The Truman Show", 1998)]
    [InlineData("The.Wolf.of.Wall.Street.2013.1080p.BluRay.x264.YIFY.mp4", "The Wolf of Wall Street", 2013)]
    [InlineData("Whats.Eating.Gilbert.Grape.1993.1080p.BluRay.x264.YIFY.mp4", "Whats Eating Gilbert Grape", 1993)]
    [InlineData("Swiss.Army.Man.2016.1080p.BluRay.x264-[YTS.AG].mp4", "Swiss Army Man", 2016)]
    [InlineData("Rocketman.2019.1080p.WEBRip.x264-[YTS.LT].mp4", "Rocketman", 2019)]
    [InlineData("28.Weeks.Later.2007.720p.BrRip.264.YIFY.mp4", "28 Weeks Later", 2007)]
    [InlineData("28.Days.Later.2002.720p.BrRip.264.YIFY.mp4", "28 Days Later", 2002)]
    [InlineData("28.Years.Later.2025.Proper.1080p.WEB-DL.DDP5.1.x265-NeoNoir.mkv", "28 Years Later", 2025)]
    [InlineData("Mad.Max.1979.1080p.BRrip.x264.YIFY.mp4", "Mad Max", 1979)]
    [InlineData("Mad.Max.2.The.Road.Warrior.1980.1080p.BRrip.x264.YIFY.mp4", "Mad Max 2 The Road Warrior", 1980)]
    [InlineData("Mad.Max.Beyond.Thunderdome.1985.1080p.BluRay.x264-[YTS.AG].mp4", "Mad Max Beyond Thunderdome", 1985)]
    [InlineData("Mad.Max.Fury.Road.2015.1080p.BluRay.x264.YIFY.mp4", "Mad Max Fury Road", 2015)]
    [InlineData("Hector.and.the.Search.for.Happiness.2014.1080p.BluRay.x264.YIFY.mp4", "Hector and the Search for Happiness", 2014)]
    [InlineData("Terminator.2.1991.1080p.BluRay.x264-[YTS.AG].mp4", "Terminator 2", 1991)]
    [InlineData("Call.Me.By.Your.Name.2017.1080p.WEBRip.x264-[YTS.AM].mp4", "Call Me By Your Name", 2017)]
    [InlineData("Green.Book.2018.1080p.BluRay.x264-[YTS.AM].mp4", "Green Book", 2018)]
    [InlineData("Lock,.Stock.and.Two.Smoking.Barrels.1998.1080p.BluRay.x264.YIFY.mp4", "Lock Stock and Two Smoking Barrels", 1998)]
    [InlineData("The.Man.From.UNCLE.2015.1080p.BluRay.x264.YIFY.[YTS.AG].mp4", "The Man From UNCLE", 2015)]
    [InlineData("King.Arthur.Legend.Of.The.Sword.2017.1080p.BluRay.x264-[YTS.AG].mp4", "King Arthur Legend Of The Sword", 2017)]
    [InlineData("Sherlock.Holmes.A.Game.Of.Shadows.2011.1080p.BrRip.x264.YIFY.mp4", "Sherlock Holmes A Game Of Shadows", 2011)]
    [InlineData("Sherlock.Holms.2009.1080p.BrRip.x264.YIFY.mp4", "Sherlock Holms", 2009)]
    [InlineData("Snatch.2000.1080p.BluRay.x264-[YTS.AM].mp4", "Snatch", 2000)]
    [InlineData("Revolver 2005.mkv", "Revolver", 2005)]
    [InlineData("The.Gentlemen.2019.1080p.WEBRip.x264.AAC5.1-[YTS.MX].mp4", "The Gentlemen", 2019)]
    [InlineData("2001.A.Space.Odyssey.1968.1080p.BluRay.x264-[YTS.AM].mp4", "2001 A Space Odyssey", 1968)]
    [InlineData("A.Clockwork.Orange.1971.1080p.BrRip.x264.YIFY.mp4", "A Clockwork Orange", 1971)]
    [InlineData("Dr..Strangelove.Or.How.I.Learned.To.Stop.Worrying.And.Love.The.Bomb.1964.1080p.BluRay.x264-[YTS.AM].mp4", "Dr Strangelove Or How I Learned To Stop Worrying And Love The Bomb", 1964)]
    [InlineData("Eyes.Wide.Shut.1999.1080p.BluRay.x264.YIFY.mp4", "Eyes Wide Shut", 1999)]
    [InlineData("Full.Metal.Jacket.1987.1080p.BluRay.x264-[YTS.AM].mp4", "Full Metal Jacket", 1987)]
    [InlineData("Lolita.1962.1080p.BluRay.x264-[YTS.AM].mp4", "Lolita", 1962)]
    [InlineData("Paths.Of.Glory.1957.1080p.BluRay.x264-[YTS.AM].mp4", "Paths Of Glory", 1957)]
    [InlineData("Spartacus.1960.1080p.BluRay.x264-[YTS.AG].mp4", "Spartacus", 1960)]
    [InlineData("The.Shining.1980.1080p.BluRay.x264-[YTS.AM].mp4", "The Shining", 1980)]
    [InlineData("Scarface.1983.1080p.BRrip.x264.YIFY.mp4", "Scarface", 1983)]
    [InlineData("Bohemian.Rhapsody.2018.1080p.BluRay.x264-[YTS.AM].mp4", "Bohemian Rhapsody", 2018)]
    [InlineData("American.Hustle.2013.1080p.BluRay.x264.YIFY.mp4", "American Hustle", 2013)]
    [InlineData("Fury.2014.1080p.BluRay.x264.YIFY.mp4", "Fury", 2014)]
    [InlineData("Joker.2019.1080p.BluRay.x264-[YTS.LT].mp4", "Joker", 2019)]
    [InlineData("Hacksaw.Ridge.2016.1080p.BluRay.x264-[YTS.AG].mp4", "Hacksaw Ridge", 2016)]
    [InlineData("Death.Proof.2007.1080p.BluRay.x264.YIFY.mp4", "Death Proof", 2007)]
    [InlineData("Django.Unchained.2012.1080p.BluRay.x264.YIFY.mp4", "Django Unchained", 2012)]
    [InlineData("Four.Rooms.1995.1080p.BluRay.x264.YIFY.mp4", "Four Rooms", 1995)]
    [InlineData("From.Dusk.Till.Dawn.1996.1080p.BrRip.x264.YIFY.mp4", "From Dusk Till Dawn", 1996)]
    [InlineData("Inglourious Bastards.2009.1080p.BrRip.x264.YIFY.mp4", "Inglourious Bastards", 2009)]
    [InlineData("The.Hateful.Eight.2015.1080p.BluRay.x264-[YTS.AG].mp4", "The Hateful Eight", 2015)]
    [InlineData("Jackie.Brown.1997.720p.BrRip.x264.YIFY.mkv", "Jackie Brown", 1997)]
    [InlineData("Kill.Bill.Vol.1.2003.1080p.BrRIp.x264.YIFY.mp4", "Kill Bill Vol 1", 2003)]
    [InlineData("Kill.Bill.Vol.2.2004.1080p.BrRIp.x264.YIFY.mp4", "Kill Bill Vol 2", 2004)]
    [InlineData("Natural.Born.Killers.1994.1080p.BluRay.x264.YIFY.mp4", "Natural Born Killers", 1994)]
    [InlineData("Pulp.Fiction.1994.1080p.BrRip.x264.YIFY.mp4", "Pulp Fiction", 1994)]
    [InlineData("Reservoir Dogs.1992.BluRay.1080p.x264.YIFY.mp4", "Reservoir Dogs", 1992)]
    [InlineData("Once.Upon.A.Time.....In.Hollywood.2019.1080p.WEBRip.x264-[YTS.LT].mp4", "Once Upon A Time In Hollywood", 2019)]
    [InlineData("Into.the.Wild.2007.1080p.BluRay.x264.YIFY.mp4", "Into the Wild", 2007)]
    [InlineData("Jojo.Rabbit.2019.1080p.BluRay.x264.AAC5.1-[YTS.MX].mp4", "Jojo Rabbit", 2019)]
    [InlineData("In.Bruges.2008.720p.BrRip.x264.YIFY.mp4", "In Bruges", 2008)]
    [InlineData("Blade.Runner.mp4", "Blade Runner", null)]
    [InlineData("Guns.Akimbo.mp4", "Guns Akimbo", null)]
    [InlineData("The.Place.Beyond.the.Pines.mp4", "The Place Beyond the Pines", null)]
    [InlineData("Fight.Club.mp4", "Fight Club", null)]
    [InlineData("Taxi.Driver.mkv", "Taxi Driver", null)]
    [InlineData("Gospodin oformitel.avi", "Gospodin oformitel", null)]
    [InlineData("Зелёный слоник.avi", "Зелёный слоник", null)]
    [InlineData("Бумер.mkv", "Бумер", null)]
    [InlineData("Requiem.For.A.Dream.mp4", "Requiem For A Dream", null)]
    [InlineData("True.History.Of.The.Kelly.Gang.mp4", "True History Of The Kelly Gang", null)]

    // Исключения
    // [InlineData("Walk.the.Line.EXTENDED.2005.1080p.BrRip.x264.YIFY.mp4", "Walk the Line", 2005)]
    // [InlineData("Rock.n.Rolla.brrip.mkv", "Rock n Rolla", null)]
    // [InlineData("Бумер Фильм второй_745.avi", "Бумер Фильм второй", null)]
    // [Theory]
    [Theory(Skip = "disabled")]
    public void ShouldCleanupTitle(string text, string exTitle, int? exYear) {
        var (title, year) = Api.Instance.ParseFileName(text);
        Assert.Equal(exTitle, title);
        Assert.Equal(exYear, year);
    }

    // [Theory]
    [Theory(Skip = "disabled")]
    [InlineData("fight club", /* 361, */ "Бойцовский клуб")]
    public async void ShouldGetShortInfo(string keyword, /* int exId, */ string exTitle) {
        var movie = new Movie();
        await Api.Instance.SuggestSearch(movie, keyword);
        // Assert.Equal(info.Id, exId);
        Assert.Equal(movie.Name, exTitle);
    }

    [Theory]
    // [Theory(Skip = "disabled")]
    [InlineData("/tmp/videos/Fight.Club.1999.1080p.BrRip.x264.YIFY.mp4", /* 361, */ "Бойцовский клуб")]
    // [InlineData("04.Сумерки. Сага. Рассвет - Часть 1 (2011) BDRip 1080p [HEVC] 10 bit.mkv", 361, "Бойцовский клуб")]
    public async void ShouldGetShortInfoFromPath(string path, /* int exId, */ string exTitle) {
        var (title, year) = Api.Instance.ParseFileName(path);
        var keyword = $"{title} {year}";
        var movie = new Movie();
        await Api.Instance.SuggestSearch(movie, keyword);
        // Assert.Equal(info.Id, exId);
        Assert.Equal(movie.Name, exTitle);
    }

    // [Fact]
    // // [Fact(Skip = "disabled")]
    // public async void ShouldReturnSomething() {
    //     var variables = new {
    //         keyword = "fight club",
    //         yandexCityId = 10777,
    //         limit = 0
    //     };

    //     var operationName = "SuggestSearch";
    //     var query = Api.GetEmbeddedQuery(operationName);
    //     var request = new { operationName, variables, query };

    //     var json = System.Text.Json.JsonSerializer.Serialize(request);
    //     var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

    //     var response = await Api.Instance._client.PostAsync("/graphql", content);
    //     response.EnsureSuccessStatusCode();
    //     var jsonString = await response.Content.ReadAsStringAsync();
    //     var result = jsonString.GetShortInfo();

    //     Assert.Equal("Бойцовский клуб", result.Title);
    //     Assert.Equal("Fight Club", result.TitleOrig);
    // }
}
