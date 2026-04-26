using MatrixEase.Web;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace shared.inctrak.com.Tests;

public class AppSettingsTests
{
    [Fact]
    public void RateLimit_DefaultsMatchMmaShape()
    {
        var settings = new AppSettings();

        Assert.NotNull(settings.RateLimit);
        Assert.False(settings.RateLimit.Enabled);
        Assert.Equal(120, settings.RateLimit.PermitLimit);
        Assert.Equal(60, settings.RateLimit.WindowSeconds);
        Assert.Equal(0, settings.RateLimit.QueueLimit);
    }

    [Fact]
    public void ErrorLogPath_DefaultsToLogsErrorsLog()
    {
        var settings = new AppSettings();

        Assert.Equal("logs/errors.log", settings.ErrorLogPath);
    }

    [Fact]
    public void LoadAppSettings_BindsOnlyFromAppSettingsSection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["MatrixEase:Web:FrontendBaseUrl"] = "https://legacy.example.com",
                ["AppSettings:FrontendBaseUrl"] = "https://frontend.example.com",
                ["AppSettings:FileSaveLocation"] = "/tmp/files"
            })
            .Build();

        var settings = Startup.LoadAppSettings(configuration);

        Assert.Equal("https://frontend.example.com", settings.FrontendBaseUrl);
        Assert.Equal("/tmp/files", settings.FileSaveLocation);
    }
}
