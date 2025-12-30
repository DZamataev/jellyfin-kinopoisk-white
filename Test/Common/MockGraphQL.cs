using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using KinopoiskWhite.Api;
using KinopoiskWhite.Api.Models;

namespace Test.Common; 


class MockGraphQL(
    ILoggerFactory loggerFactory,
    IHttpClientFactory httpClientFactory
) : GraphQL(loggerFactory, httpClientFactory)
{
    public static string MockGetEmbeddedQuery(string fileName)
    => GetEmbeddedQuery(fileName);

    public async Task<JsonElement> MockCall(string operationName, object variables,
                                            CancellationToken cancellationToken)
    => await Call(operationName, variables, cancellationToken);

    public async Task<JsonElement> MockCallApi(string method, CancellationToken cancellationToken)
    => await CallApi(method, cancellationToken);

    public static JsonElement MockWalk(JsonElement root, string path) => Walk(root, path);

    public async Task<JsonElement> MockCall(string operationName, object variables, string path,
                                            CancellationToken cancellationToken)
    => await Call(operationName, variables, path, cancellationToken);

    public async Task<FilmInfo> MockCallAndDeserialize(
        string operationName, object variables, string path,
        CancellationToken cancellationToken)
    => await CallAndDeserialize<FilmInfo>(operationName, variables, path, cancellationToken);
}