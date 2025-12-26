using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Concurrent;

namespace Test.Common;

using Response = System.Func<HttpRequestMessage, HttpResponseMessage>;

class MockHttpClientFactory(HttpMessageHandler handler)
: IHttpClientFactory
{
    readonly HttpMessageHandler _handler = handler;

    public HttpClient CreateClient(string name = null) => new(_handler);
    public HttpMessageHandler CreateHandler(string _) => _handler;

    public class MessageHandler()
    : HttpMessageHandler
    {
        ConcurrentStack<Response> Responses = new();
        readonly object _lock = new();

        public void SetResponses(Response[] responses)
        {
            lock(_lock)
            {
                Responses.Clear();
                foreach (var response in responses.Reverse())
                    Responses.Push(response);
            }
        }

        protected override Task<HttpResponseMessage>
        SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            lock(_lock)
                if (Responses.TryPop(out var response))
                    return Task.FromResult(response(request));

            return Task.FromResult(new HttpResponseMessage{ StatusCode = HttpStatusCode.NotFound });
        }
    }
}