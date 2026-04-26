using MatrixEase.Web;
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
}
