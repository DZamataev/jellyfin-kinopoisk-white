using System.Net;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Test.Common;

using Response = System.Func<HttpRequestMessage, HttpResponseMessage>;

class MockHttpClientFactory(HttpMessageHandler handler, IHttpMessageHandlerFactory factory)
: IHttpClientFactory
{
    readonly HttpMessageHandler _handler = handler;
    readonly IHttpMessageHandlerFactory _factory = factory;

    public HttpClient CreateClient(string name = null)
    => new(_handler);
    public HttpMessageHandler CreateHandler(string name)
    => _factory?.CreateHandler(name) ?? _handler;

    class MessageHandler(Response[] responses)
    : HttpMessageHandler
    {
        readonly Stack<Response> _responses = new(responses.Reverse());

        protected override Task<HttpResponseMessage>
        SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_responses.TryPop(out var response))
                return Task.FromResult(response(request));

            return Task.FromResult(new HttpResponseMessage{ StatusCode = HttpStatusCode.NotFound });
        }
    }
}