using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Plugin.Providers;

using Api;
using Common;

public abstract class BaseProvider
{
    #pragma warning disable CA1822 // Mark members as static
    public string Name => Constants.ProviderName;
    public string Description => Constants.ProviderDescription;
    #pragma warning restore CA1822 // Mark members as static

    protected readonly ILogger _logger;
    protected readonly IHttpClientFactory _httpClientFactory;
    protected readonly KinopoiskApi _api;

    public BaseProvider(
        ILogger<BaseProvider> logger,
        IHttpClientFactory httpClientFactory,
        KinopoiskApi api = null)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _api = api ?? new KinopoiskApi(httpClientFactory);
    }

    public Task<HttpResponseMessage>
    GetImageResponse(string url, CancellationToken cancellationToken)
    => _httpClientFactory
        .CreateClient(MediaBrowser.Common.Net.NamedClient.Default)
        .GetAsync(url, cancellationToken);
}