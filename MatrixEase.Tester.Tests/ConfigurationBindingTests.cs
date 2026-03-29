using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace MatrixEase.Tester.Tests;

public class ConfigurationBindingTests
{
    [Fact]
    public void WebAppSettings_BindFromMatrixEaseWebSection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["MatrixEase:Web:FileSaveLocation"] = "/tmp/matrixease-web",
                ["MatrixEase:Web:ProtectionKey"] = "test-protection-key",
                ["MatrixEase:Web:GoogleClientId"] = "web-client-id",
                ["MatrixEase:Web:MaxConcurrentJobs"] = "12",
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<MatrixEase.Web.AppSettings>(configuration.GetSection("MatrixEase:Web"));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<MatrixEase.Web.AppSettings>>().Value;

        Assert.Equal("/tmp/matrixease-web", options.FileSaveLocation);
        Assert.Equal("test-protection-key", options.ProtectionKey);
        Assert.Equal("web-client-id", options.GoogleClientId);
        Assert.Equal(12, options.MaxConcurrentJobs);
    }

    [Fact]
    public void DesktopAppSettings_BindFromMatrixEaseAppSection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["MatrixEase:App:GoogleClientId"] = "desktop-client-id",
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<Desktop.MatrixEase.Manga.AppSettings>(configuration.GetSection("MatrixEase:App"));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<Desktop.MatrixEase.Manga.AppSettings>>().Value;

        Assert.Equal("desktop-client-id", options.GoogleClientId);
    }
}
