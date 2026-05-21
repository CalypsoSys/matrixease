using MatrixEase.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace MatrixEase.Web.Tests;

public class AppSettingsTests
{
    [Fact]
    public void BindsApiFrontendSupabaseAndSlackSettings()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["MatrixEase:Web:FileSaveLocation"] = "/srv/stacks/matrixease/data",
                ["MatrixEase:Web:FrontendBaseUrl"] = "https://app.matrixease.com",
                ["MatrixEase:Web:AllowedOrigins:0"] = "https://app.matrixease.com",
                ["MatrixEase:Web:AllowedOrigins:1"] = "http://localhost:5173",
                ["MatrixEase:Web:RequireGatewaySecret"] = "true",
                ["MatrixEase:Web:GatewaySecretHeaderName"] = "X-MatrixEase-Gateway",
                ["MatrixEase:Web:GatewaySecret"] = "local-secret",
                ["MatrixEase:Web:RateLimit:Enabled"] = "true",
                ["MatrixEase:Web:RateLimit:PermitLimit"] = "33",
                ["MatrixEase:Web:RateLimit:WindowSeconds"] = "44",
                ["MatrixEase:Web:RateLimit:QueueLimit"] = "2",
                ["MatrixEase:Web:SupabaseUrl"] = "https://project-ref.supabase.co",
                ["MatrixEase:Web:SupabaseAnonKey"] = "sb_publishable_test",
                ["MatrixEase:Web:SupabaseJwtSecret"] = "legacy-jwt-secret",
                ["MatrixEase:Web:SlackFeedbackWebhookUrl"] = "https://hooks.slack.com/services/test",
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<AppSettings>(configuration.GetSection("MatrixEase:Web"));

        using var provider = services.BuildServiceProvider();
        AppSettings settings = provider.GetRequiredService<IOptions<AppSettings>>().Value;

        Assert.Equal("/srv/stacks/matrixease/data", settings.FileSaveLocation);
        Assert.Equal("https://app.matrixease.com", settings.FrontendBaseUrl);
        Assert.Equal(new[] { "https://app.matrixease.com", "http://localhost:5173" }, settings.AllowedOrigins);
        Assert.True(settings.RequireGatewaySecret);
        Assert.Equal("X-MatrixEase-Gateway", settings.GatewaySecretHeaderName);
        Assert.Equal("local-secret", settings.GatewaySecret);
        Assert.True(settings.RateLimit.Enabled);
        Assert.Equal(33, settings.RateLimit.PermitLimit);
        Assert.Equal(44, settings.RateLimit.WindowSeconds);
        Assert.Equal(2, settings.RateLimit.QueueLimit);
        Assert.Equal("https://project-ref.supabase.co", settings.GetSupabaseUrl());
        Assert.Equal("sb_publishable_test", settings.GetSupabaseAnonKey());
        Assert.Equal("legacy-jwt-secret", settings.GetSupabaseJwtSecret());
        Assert.Equal("https://hooks.slack.com/services/test", settings.GetSlackFeedbackWebhookUrl());
    }
}
