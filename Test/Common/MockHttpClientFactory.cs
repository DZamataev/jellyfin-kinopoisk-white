using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Test.Common;

using Response = System.Func<HttpRequestMessage, HttpResponseMessage>;

class MockHttpClientFactory(HttpMessageHandler handler)
: IHttpClientFactory
{
    readonly HttpMessageHandler _handler = handler;

    public HttpClient CreateClient(string name = null) => new(_handler);
    public HttpMessageHandler CreateHandler(string name) => _handler;

    public class MessageHandler()
    : HttpMessageHandler
    {
        Stack<Response> Responses = new();

        public void SetResponses(Response[] responses)
        {
            Responses = new([.. responses.Reverse()]);
        }

        protected override Task<HttpResponseMessage>
        SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Responses.TryPop(out var response))
                return Task.FromResult(response(request));

            return Task.FromResult(new HttpResponseMessage{ StatusCode = HttpStatusCode.NotFound });
        }
    }
}