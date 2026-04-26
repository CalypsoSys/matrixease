using MatrixEase.Web.Common;
using System.Net;
using System.Net.Http;
using Xunit;

namespace shared.inctrak.com.Tests;

public class SlackWebhookNotifierTests
{
    [Fact]
    public void BuildPayload_ProducesSlackTextJson()
    {
        var payload = SlackWebhookNotifier.BuildPayload("hello");

        Assert.Equal("{\"text\":\"hello\"}", payload);
    }

    [Fact]
    public void Send_ThrowsWhenWebhookMissing()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            SlackWebhookNotifier.Send("", "hello", new HttpClient(new StubHttpMessageHandler((_, _) =>
                new HttpResponseMessage(HttpStatusCode.OK)))));

        Assert.Equal("AppSettings__SlackFeedbackWebhookUrl must be configured for Slack feedback notifications.", exception.Message);
    }

    [Fact]
    public void Send_PostsSlackPayload()
    {
        string requestBody = null;
        string requestUrl = null;

        var httpClient = new HttpClient(new StubHttpMessageHandler((request, body) =>
        {
            requestUrl = request.RequestUri.ToString();
            requestBody = body;
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));

        SlackWebhookNotifier.Send("https://hooks.slack.test/services/abc", "hello", httpClient);

        Assert.Equal("https://hooks.slack.test/services/abc", requestUrl);
        Assert.Equal("{\"text\":\"hello\"}", requestBody);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, string, HttpResponseMessage> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, string, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string body = request.Content == null
                ? string.Empty
                : request.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();

            return Task.FromResult(_handler(request, body));
        }
    }
}
