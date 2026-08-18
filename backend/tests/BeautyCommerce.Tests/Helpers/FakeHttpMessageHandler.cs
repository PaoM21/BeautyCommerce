using System.Net;

namespace BeautyCommerce.Tests.Helpers;

/// <summary>
/// HttpMessageHandler de prueba: captura la última request enviada y
/// responde con lo que se configure, sin hacer ninguna llamada real de red.
/// </summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _responseBody;

    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }

    public FakeHttpMessageHandler(
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string responseBody = "{}")
    {
        _statusCode = statusCode;
        _responseBody = responseBody;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;

        LastRequestBody = request.Content == null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        return new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseBody),
        };
    }
}
