using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace KinopoiskWhite.Providers;

using Api;
using Api.Models;

public abstract class BaseProvider<TMetadata>: BaseSingleton
where TMetadata : BaseMetadata
{
    protected readonly IApiService<TMetadata> _api;

    protected BaseProvider(ILogger logger, IHttpClientFactory httpClientFactory, IApiService<TMetadata> api)
    : base(logger, httpClientFactory)
    {
        _api = api;
    }

    public Task<HttpResponseMessage>
    GetImageResponse(string url, CancellationToken cancellationToken)
    => _httpClientFactory
        .CreateClient(MediaBrowser.Common.Net.NamedClient.Default)
        .GetAsync(url, cancellationToken);
}