using System.Net;
using MatrixEase.Web.Middleware;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace MatrixEase.Web.Tests;

public class AccessLogMiddlewareTests
{
    [Fact]
    public void BuildLogLineUsesCloudflareIpAndKeepsOneLine()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.5");
        context.Request.Headers["CF-Connecting-IP"] = "203.0.113.8";
        context.Request.Headers.Referer = "https://app.matrixease.com/upload";
        context.Request.Headers.UserAgent = "MatrixEase\r\nTest";
        context.Request.Method = "POST";
        context.Request.Path = "/api/feedback/save_message/";
        context.Request.QueryString = new QueryString("?q=one%0Atwo");
        context.Request.Protocol = "HTTP/1.1";
        context.Response.StatusCode = StatusCodes.Status202Accepted;

        string line = AccessLogMiddleware.BuildLogLine(context, 17);

        Assert.Contains("203.0.113.8", line);
        Assert.Contains("\"POST /api/feedback/save_message/?q=one%0Atwo HTTP/1.1\" 202", line);
        Assert.Contains("MatrixEase  Test", line);
        Assert.DoesNotContain("\r", line);
        Assert.DoesNotContain("\n", line);
    }
}
