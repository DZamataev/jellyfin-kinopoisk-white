using Xunit;

using Microsoft.Extensions.DependencyInjection;

using KinopoiskWhite.Providers;

namespace Test;


// class TaskQueueTestsException() : System.Exception("Task queue test exception") {}

public class MovieProviderTests
{
    private readonly MovieProvider provider;

    public MovieProviderTests()
    {

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient();
        // serviceCollection.AddLogging(builder => {
        //     builder.AddConsole();
        //     builder.AddFilter("KinopoiskWhite.Api.GraphQL", LogLevel.Trace);
        // });
        // serviceCollection.AddSingleton<IGraphQL, GraphQL>();
    }

    [Fact]
    public void CheckRegistration()
    {
        System.Console.WriteLine("Ok");
    }
}