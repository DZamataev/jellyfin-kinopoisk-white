using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MediaBrowser.Controller.Providers;

using KinopoiskWhite.Api;
using KinopoiskWhite.Providers;
using KinopoiskWhite.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;

namespace App; 

class Program {
    private static IApiService _api;
    private static IRemoteImageProvider _imageProvider;
    private static IRemoteMetadataProvider<Movie, MovieInfo> _movieProvider;
    private static IRemoteMetadataProvider<Person, PersonLookupInfo> _personProvider;
    private static readonly CancellationToken _token = CancellationToken.None;
    
    private static void Prepare()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient();
        serviceCollection.AddLogging(builder => {
            builder.AddConsole();
            builder.AddFilter("KinopoiskWhite.Api.GraphQL", LogLevel.Trace);
        });
        serviceCollection.AddSingleton<IGraphQL, GraphQL>();
        serviceCollection.AddSingleton<IApiService, ApiService>();
        serviceCollection.AddSingleton<IRemoteImageProvider, PersonImageProvider>();
        serviceCollection.AddSingleton<IRemoteMetadataProvider<Movie, MovieInfo>, MovieMetadataProvider>();
        serviceCollection.AddSingleton<IRemoteMetadataProvider<Person, PersonLookupInfo>, PersonMetadataProvider>();

        var sp = serviceCollection.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true }
        );

        _api = sp.GetRequiredService<IApiService>();
        _imageProvider = sp.GetRequiredService<IRemoteImageProvider>();
        _movieProvider = sp.GetRequiredService<IRemoteMetadataProvider<Movie, MovieInfo>>();
        _personProvider = sp.GetRequiredService<IRemoteMetadataProvider<Person, PersonLookupInfo>>();

        // _api = new KinopoiskApi(
        //     sp.GetRequiredService<IHttpClientFactory>()
        // );
        // _api = new ApiService(null);
    }

    static async Task Main(string[] args) {
        Prepare();
        Console.WriteLine("Started");
        // "Девушка в тумане (2017) BDRip-AVC_ivanes20031987.mkv".ParseFileName();
        // "04.Сумерки. Сага. Рассвет - Часть 1 (2011) BDRip 1080p [HEVC] 10 bit.mkv".ParseFileName();
        "Idiocracy.2006.HDTV.720p.x264.YIFY.mp4".ParseFileName();

        var info = new MovieInfo
        {
            Path = "Fight Club 1999.mkv"
            // Path = "Idiocracy.2006.HDTV.720p.x264.YIFY.mp4"
            // Path = "F1. The Movie (2025).mkv"
            // Path = "After.Life.1998.HDRip_[1.46].avi"
            // Path = "Other.2025.DUB.WEB-DLRip-AVC.x264.seleZen.mkv"
        };
        // var meta = await _api.GetKinopoiskId(info.Path, _token);
        // meta = await _api.Fetch(meta.Id, _token);
        // meta = await _api.GetImages(meta.Id, _token);
        // Console.WriteLine($"{meta.Id}");

        // var item = new Movie();
        // item.SetDefaultId(361);
        // var images = await _imageProvider.GetImages(item, _token);
        // foreach (var image in images)
        //     Console.WriteLine($"{image}");

        var item = new PersonLookupInfo();
        item.SetDefaultId(419797);
        // item.Name = "арата иура";
        var result = await _personProvider.GetMetadata(item, _token);
        Console.WriteLine($"{result.Item.Name}");
        // var images = await _imageProvider.GetImages(item, _token);
        // foreach (var image in images)
        //     Console.WriteLine($"{image.Url}");

        // var item = new MovieInfo();
        // item.SetDefaultId(361);
        // item.Name = "fight club";
        // item.Year = 1999;
        // var results = await _movieProvider.GetSearchResults(item, _token);
        // foreach (var result in results)
        //     Console.WriteLine($"{result}");

        // var person = await _api.GetPerson(25774, _token);
        // Console.WriteLine($"{person}");

        Console.WriteLine("Finished");
    }
}