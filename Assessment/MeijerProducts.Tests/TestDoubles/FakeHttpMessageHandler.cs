using System.Net;
using System.Text;

namespace MeijerProducts.Tests.TestDoubles;

// Stands in for a real network round-trip so ProductService can be exercised against
// canned responses. Captures requests so tests can assert the composed URI.
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public List<HttpRequestMessage> Requests { get; } = [];

    public HttpRequestMessage LastRequest => Requests[^1];

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    public static FakeHttpMessageHandler RespondingWithJson(HttpStatusCode statusCode, string json) =>
        new(_ => new HttpResponseMessage(statusCode)
        {
            // Without the JSON media type, ReadFromJsonAsync throws NotSupportedException
            // instead of deserializing.
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

    public static FakeHttpMessageHandler RespondingWith(HttpStatusCode statusCode) =>
        new(_ => new HttpResponseMessage(statusCode));

    // Wraps the handler in an HttpClient with the same trailing-slash base address shape
    // MauiProgram uses, so relative request URIs resolve the way they do in the app.
    public HttpClient CreateClient() =>
        new(this) { BaseAddress = new Uri("http://localhost:5217/") };

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Requests.Add(request);

        return Task.FromResult(_responder(request));
    }
}
