using MatrixEase.Web;
using MatrixEase.Web.Common;
using Xunit;

namespace shared.inctrak.com.Tests;

public class ErrorLogWriterTests
{
    [Fact]
    public void BuildErrorEntry_FormatsMessageAndExceptions()
    {
        var exception = new InvalidOperationException(
            "outer problem",
            new Exception("inner problem"));

        string entry = ErrorLogWriter.BuildErrorEntry(
            "abc12345",
            exception,
            "Failed action for {0}",
            "demo");

        Assert.Contains("code=abc12345", entry);
        Assert.Contains("message=Failed action for demo", entry);
        Assert.Contains("exception: outer problem", entry);
        Assert.Contains("inner_exception_1: inner problem", entry);
    }

    [Fact]
    public void BuildStartupExceptionEntry_FormatsStartupFailure()
    {
        string entry = ErrorLogWriter.BuildStartupExceptionEntry(
            new InvalidOperationException("startup blew up"));

        Assert.Contains("startup_exception", entry);
        Assert.Contains("startup blew up", entry);
    }

    [Fact]
    public void LogError_ReturnsShortErrorCode()
    {
        string errorCode = ErrorLogWriter.LogError(
            new AppSettings
            {
                ErrorLogPath = "/tmp/shared.inctrak.error-test.log"
            },
            new InvalidOperationException("failure"),
            "Background task failed");

        Assert.Equal(8, errorCode.Length);
    }
}
