namespace DataIngestor.Tests.UnitTests.TestUtils;

/// <summary>
/// Stub <see cref="HttpMessageHandler"/> used to fake HTTP responses for <see cref="HttpClient"/> dependants
/// without performing real network calls.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _send;

    public List<HttpRequestMessage> SentRequests { get; } = new();

    public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
    {
        _send = send;
    }

    public static StubHttpMessageHandler ReturnsJson(string json, System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
        => new((_, _) => Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        }));

    public static StubHttpMessageHandler Throws(Exception ex)
        => new((_, _) => throw ex);

    public static StubHttpMessageHandler WithStatus(System.Net.HttpStatusCode statusCode)
        => new((_, _) => Task.FromResult(new HttpResponseMessage(statusCode)));

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        SentRequests.Add(request);
        return _send(request, cancellationToken);
    }
}
