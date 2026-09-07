using Moq;
using Moq.Protected;

using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace Test.Common;

public class MockHttpClientFactory: Mock<IHttpClientFactory>
{
    public MockHttpClientFactory()
    {
        Setup(f => f.CreateClient(It.IsAny<string>())).Returns(HttpClient);
    }

    public void SetResponses(HttpResponseMessage[] responses)
    {
        Responses.Clear();
        foreach (var response in responses.Reverse())
            Responses.Push(response);
    }

    public void SetResponse(HttpResponseMessage response) => SetResponses([response]);

    // Body of the most recent outgoing request, so tests can assert on what was sent
    // (e.g. that SuggestSearch carries yandexCityId).
    public string LastRequestBody { get; private set; }

    readonly Stack<HttpResponseMessage> Responses = new();
    HttpClient HttpClient {
        get {
            var handler = new Mock<HttpMessageHandler>();

            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
                    LastRequestBody = request.Content?.ReadAsStringAsync().Result)
                .ReturnsAsync(() => {
                    if (Responses.TryPop(out var response))
                        return response;

                    return new() { StatusCode = HttpStatusCode.NotFound };
                });

            return new HttpClient(handler.Object);
        }
    }
}