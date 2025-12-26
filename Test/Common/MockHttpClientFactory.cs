using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Test.Common;

class MockHttpClientFactory(HttpMessageHandler handler)
: IHttpClientFactory
{
    readonly HttpMessageHandler _handler = handler;

    public HttpClient CreateClient(string name = null) => new(_handler);
    public HttpMessageHandler CreateHandler(string name) => _handler;

    public class MessageHandler()
    : HttpMessageHandler
    {
        public Stack<System.Func<HttpRequestMessage, HttpResponseMessage>> Responses = new();

        protected override Task<HttpResponseMessage>
        SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Responses.TryPop(out var response))
                return Task.FromResult(response(request));

            return Task.FromResult(new HttpResponseMessage{ StatusCode = HttpStatusCode.NotFound });
        }
    }
}