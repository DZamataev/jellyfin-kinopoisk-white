using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;

using KinopoiskWhite.Api;
using KinopoiskWhite.Extensions;

namespace App; 

class Program {
    private static ApiService _api;
    private static readonly CancellationToken _token = CancellationToken.None;
    
    private static void Prepare()
    {
        // var services = new ServiceCollection();
        // services.AddHttpClient();
        // var sp = services.BuildServiceProvider();

        // _api = new KinopoiskApi(
        //     sp.GetRequiredService<IHttpClientFactory>()
        // );
        _api = new ApiService(null);
    }

    static async Task Main(string[] args) {
        Prepare();
        Console.WriteLine("Started");
        // "Девушка в тумане (2017) BDRip-AVC_ivanes20031987.mkv".ParseFileName();
        // "04.Сумерки. Сага. Рассвет - Часть 1 (2011) BDRip 1080p [HEVC] 10 bit.mkv".ParseFileName();
        "Idiocracy.2006.HDTV.720p.x264.YIFY.mp4".ParseFileName();

        var info = new MovieInfo
        {
            // Path = "Idiocracy.2006.HDTV.720p.x264.YIFY.mp4"
            // Path = "F1. The Movie (2025).mkv"
            // Path = "After.Life.1998.HDRip_[1.46].avi"
            Path = "Other.2025.DUB.WEB-DLRip-AVC.x264.seleZen.mkv"
        };
        var meta = await _api.GetKinopoiskId(info.Path, _token);
        meta = await _api.Fetch(meta.Id, _token);
        Console.WriteLine($"{meta.Title}");
        meta = await _api.FetchByContentId(meta.ContentId, _token);
        Console.WriteLine($"{meta.Title}");
        var result = new MetadataResult<Movie>()
        {
            Item = new Movie()
        };
        result.FillFrom(meta);
        Console.WriteLine("Finished");
    }
}