using MatrixEase.Web.Common;
using MatrixEase.Web.Controllers;
using Xunit;

namespace MatrixEase.Web.Tests;

public class FeedbackControllerTests
{
    [Fact]
    public void BuildFeedbackSlackMessageEscapesSlackSpecialCharacters()
    {
        string message = FeedbackController.BuildFeedbackSlackMessage(new Feedback
        {
            Name = "Joe <Admin>",
            EmailAddress = "joe@example.com",
            Subject = "Hello & help",
            ClientData = "203.0.113.8",
            Message = "Please inspect > export",
        });

        Assert.Contains("*MatrixEase feedback*", message);
        Assert.Contains("Joe &lt;Admin&gt;", message);
        Assert.Contains("Hello &amp; help", message);
        Assert.Contains("Please inspect &gt; export", message);
        Assert.DoesNotContain("Joe <Admin>", message);
    }
}
