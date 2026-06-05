using System.Net;

namespace GoalsBot.Tests.Llm;

internal sealed class StubHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = new();
    public List<string> CapturedBodies { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        if (request.Content is not null)
            CapturedBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
        return await handler(request, cancellationToken);
    }

    public static StubHttpMessageHandler RespondingWithContent(string responseContent, HttpStatusCode status = HttpStatusCode.OK) =>
        new((_, _) => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(responseContent, System.Text.Encoding.UTF8, "application/json")
        }));
}
