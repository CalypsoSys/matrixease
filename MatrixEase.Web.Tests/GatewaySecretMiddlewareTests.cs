using MatrixEase.Web.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace MatrixEase.Web.Tests;

public class GatewaySecretMiddlewareTests
{
    [Theory]
    [InlineData("/api/feedback/save_message/")]
    [InlineData("/api/matrixease/upload")]
    public void ShouldRequireGatewaySecretForApiRoutes(string path)
    {
        Assert.True(GatewaySecretMiddleware.ShouldRequireGatewaySecret(path));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/index.html")]
    [InlineData("/images/logo.png")]
    [InlineData("/google/check_login")]
    [InlineData("/account/login")]
    [InlineData("/upload_file/")]
    [InlineData("/get_access")]
    public void ShouldNotRequireGatewaySecretForStaticRoutes(string path)
    {
        Assert.False(GatewaySecretMiddleware.ShouldRequireGatewaySecret(path));
    }

    [Fact]
    public async Task InvokeReturnsUnauthorizedWhenSecretIsMissing()
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/feedback/save_message/";

        await middleware.Invoke(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeCallsNextWhenSecretMatches()
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/feedback/save_message/";
        context.Request.Headers["X-Internal-Api-Key"] = "test-gateway-secret";

        await middleware.Invoke(context);

        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeSkipsStaticRoutes()
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Path = "/index.html";

        await middleware.Invoke(context);

        Assert.True(nextCalled);
    }

    private static GatewaySecretMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new GatewaySecretMiddleware(
            next,
            Options.Create(new AppSettings
            {
                RequireGatewaySecret = true,
                GatewaySecretHeaderName = "X-Internal-Api-Key",
                GatewaySecret = "test-gateway-secret",
            }),
            new TestWebHostEnvironment
            {
                EnvironmentName = Environments.Production,
            });
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "MatrixEase.Web.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "";
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
