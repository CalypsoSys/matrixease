using Desktop.MatrixEase.Manga;
using Xunit;

namespace MatrixEase.Tester.Tests;

public class AppSettingsTests
{
    [Fact]
    public void WebAppSettings_ExposeConfiguredValues()
    {
        var settings = new MatrixEase.Web.AppSettings
        {
            FileSaveLocation = "/srv/stacks/matrixease/data",
            ProtectionKey = "protection-key",
            FrontendBaseUrl = "https://app.matrixease.com",
            AllowedOrigins = new[] { "https://app.matrixease.com", "http://localhost:5173" },
            RequireGatewaySecret = true,
            GatewaySecretHeaderName = "X-MatrixEase-Gateway",
            GatewaySecret = "gateway-secret",
            RateLimit = new MatrixEase.Web.RateLimitSettings
            {
                Enabled = true,
                PermitLimit = 33,
                WindowSeconds = 44,
                QueueLimit = 2,
            },
            AccessLogPath = "/app/logs/access.log",
            ErrorLogPath = "/app/logs/errors.log",
            SupabaseUrl = "https://project-ref.supabase.co",
            SupabaseAnonKey = "sb_publishable_test",
            SupabaseJwtSecret = "legacy-jwt-secret",
            SupabaseAudience = "authenticated",
            SlackFeedbackWebhookUrl = "https://hooks.slack.com/services/test",
        };

        Assert.Equal("/srv/stacks/matrixease/data", settings.FileSaveLocation);
        Assert.Equal("protection-key", settings.ProtectionKey);
        Assert.Equal("https://app.matrixease.com", settings.FrontendBaseUrl);
        Assert.Equal(new[] { "https://app.matrixease.com", "http://localhost:5173" }, settings.AllowedOrigins);
        Assert.True(settings.RequireGatewaySecret);
        Assert.Equal("X-MatrixEase-Gateway", settings.GatewaySecretHeaderName);
        Assert.Equal("gateway-secret", settings.GatewaySecret);
        Assert.True(settings.RateLimit.Enabled);
        Assert.Equal(33, settings.RateLimit.PermitLimit);
        Assert.Equal(44, settings.RateLimit.WindowSeconds);
        Assert.Equal(2, settings.RateLimit.QueueLimit);
        Assert.Equal("/app/logs/access.log", settings.GetAccessLogPath());
        Assert.Equal("/app/logs/errors.log", settings.GetErrorLogPath());
        Assert.Equal("https://project-ref.supabase.co", settings.GetSupabaseUrl());
        Assert.Equal("sb_publishable_test", settings.GetSupabaseAnonKey());
        Assert.Equal("legacy-jwt-secret", settings.GetSupabaseJwtSecret());
        Assert.Equal("authenticated", settings.GetSupabaseAudience());
        Assert.Equal("https://hooks.slack.com/services/test", settings.GetSlackFeedbackWebhookUrl());
    }

    [Fact]
    public void WebAppSettings_DefaultMaxConcurrentJobsToTen()
    {
        var settings = new MatrixEase.Web.AppSettings();

        Assert.Equal(10, settings.MaxConcurrentJobs);
    }

    [Fact]
    public void DesktopAppSettings_ExposeConfiguredValues()
    {
        var settings = new AppSettings
        {
            GoogleClientId = "google-client-id",
        };

        Assert.Equal("google-client-id", settings.GoogleClientId);
    }
}
