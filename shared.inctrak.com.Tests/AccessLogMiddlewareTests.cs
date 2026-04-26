using MatrixEase.Web.Middleware;
using Microsoft.AspNetCore.Http;
using System.Net;
using Xunit;

namespace shared.inctrak.com.Tests;

public class AccessLogMiddlewareTests
{
    [Fact]
    public void GetRemoteIp_PrefersCloudflareHeader()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["CF-Connecting-IP"] = "203.0.113.4";
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.12");

        var remoteIp = AccessLogMiddleware.GetRemoteIp(context);

        Assert.Equal("203.0.113.4", remoteIp);
    }

    [Fact]
    public void BuildLogLine_FormatsCombinedStyleEntry()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/test";
        context.Request.QueryString = new QueryString("?page=1");
        context.Request.Protocol = "HTTP/1.1";
        context.Request.Headers["Referer"] = "https://example.test/source";
        context.Request.Headers["User-Agent"] = "Test Agent";
        context.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.25");
        context.Response.StatusCode = 200;
        context.Response.ContentLength = 123;

        var line = AccessLogMiddleware.BuildLogLine(
            context,
            elapsedMilliseconds: 42,
            now: new DateTimeOffset(2026, 4, 26, 14, 30, 0, TimeSpan.FromHours(-4)));

        Assert.Equal(
            "198.51.100.25 - - [26/Apr/2026:14:30:00 -04:00] \"GET /api/test?page=1 HTTP/1.1\" 200 123 \"https://example.test/source\" \"Test Agent\" 42ms",
            line);
    }
}
