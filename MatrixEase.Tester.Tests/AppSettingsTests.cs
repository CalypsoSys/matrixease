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
            ProtectionKey = "protection-key",
            GoogleClientId = "google-client-id",
            GoogleClientSecret = "google-client-secret",
            SNMPServer = "smtp.example.test",
            SNMPAddress = "noreply@example.test",
            SNMPPassword = "smtp-password",
            EmailApiKey = "sendgrid-key",
            EmailFrom = "feedback@example.test",
        };

        Assert.Equal("protection-key", settings.ProtectionKey);
        Assert.Equal("google-client-id", settings.GoogleClientId);
        Assert.Equal("google-client-secret", settings.GoogleClientSecret);
        Assert.Equal("smtp.example.test", settings.SNMPServer);
        Assert.Equal("noreply@example.test", settings.SNMPAddress);
        Assert.Equal("smtp-password", settings.SNMPPassword);
        Assert.Equal("sendgrid-key", settings.EmailApiKey);
        Assert.Equal("feedback@example.test", settings.EmailFrom);
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
