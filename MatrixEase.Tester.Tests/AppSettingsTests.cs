using Desktop.MatrixEase.Manga;
using Xunit;

namespace MatrixEase.Tester.Tests;

public class AppSettingsTests
{
    [Fact]
    public void WebAppSettings_ReturnPlainConfiguredValues()
    {
        var settings = new MatrixEase.Web.AppSettings
        {
            GoogleClientId = "google-client-id",
            GoogleClientSecret = "google-client-secret",
            SNMPServer = "smtp.example.test",
            SNMPAddress = "noreply@example.test",
            SNMPPassword = "smtp-password",
            EmailApiKey = "sendgrid-key",
            EmailFrom = "feedback@example.test",
        };

        Assert.Equal("google-client-id", settings.GetGoogleClientId());
        Assert.Equal("google-client-secret", settings.GetGoogleClientSecret());
        Assert.Equal("smtp.example.test", settings.GetSNMPServer());
        Assert.Equal("noreply@example.test", settings.GetSNMPAddress());
        Assert.Equal("smtp-password", settings.GetSNMPPassword());
        Assert.Equal("sendgrid-key", settings.GetEmailApiKey());
        Assert.Equal("feedback@example.test", settings.GetEmailFrom());
    }

    [Fact]
    public void DesktopAppSettings_ReturnPlainConfiguredValues()
    {
        var settings = new AppSettings
        {
            GoogleClientId = "google-client-id",
            GoogleClientSecret = "google-client-secret",
        };

        Assert.Equal("google-client-id", settings.GetGoogleClientId());
        Assert.Equal("google-client-secret", settings.GetGoogleClientSecret());
    }
}
