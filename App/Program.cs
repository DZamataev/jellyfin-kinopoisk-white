using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.KinopoiskWhite; 

using Api;
using Extensions;

class Program {
    private static KinopoiskApi _api;
    
    private static void Prepare()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        var sp = services.BuildServiceProvider();

        _api = new KinopoiskApi(
            new GraphQL(
                sp.GetRequiredService<ILogger<GraphQL>>(),
                sp.GetRequiredService<IHttpClientFactory>()
            ),
            sp.GetRequiredService<ILogger<KinopoiskApi>>(),
            sp.GetRequiredService<IHttpClientFactory>()
        );
    }

    static async Task Main(string[] args) {
        Prepare();
        Console.WriteLine("Started");
        // "Девушка в тумане (2017) BDRip-AVC_ivanes20031987.mkv".ParseFileName();
        // "04.Сумерки. Сага. Рассвет - Часть 1 (2011) BDRip 1080p [HEVC] 10 bit.mkv".ParseFileName();
        "Idiocracy.2006.HDTV.720p.x264.YIFY.mp4".ParseFileName();

        var info = new MovieInfo
        {
            Path = "Idiocracy.2006.HDTV.720p.x264.YIFY.mp4"
        };
        var kid = await _api.GetKinopoiskId(info, CancellationToken.None);
        Console.WriteLine("Finished");
    }
}