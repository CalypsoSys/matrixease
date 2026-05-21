using MatrixEase.Web.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MatrixEase.Web.Controllers
{
    [ApiController]
    public class FeedbackController : ControllerBase
    {
        private readonly IOptions<AppSettings> _options;
        private readonly ILogger<FeedbackController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public FeedbackController(IOptions<AppSettings> options, ILogger<FeedbackController> logger, IHttpClientFactory httpClientFactory)
        {
            _options = options;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        [Route("api/feedback/save_message/")]
        [HttpPost]
        public async Task<ActionResult> Post(Feedback feedBack)
        {
            try
            {
                Uri referrer = null;
                if (Request.Headers.TryGetValue("Referer", out StringValues header))
                    Uri.TryCreate(header.ToString(), UriKind.Absolute, out referrer);

                if (referrer == null ||
                    (referrer.DnsSafeHost.EndsWith("matrixease.com") == false
#if DEBUG 
                    && referrer.DnsSafeHost != "localhost"
#endif
                    ))
                {
                    return Ok(new { success = false, message = "Invalid request" });
                }

                if (feedBack == null || (string.IsNullOrWhiteSpace(feedBack.EmailAddress) && string.IsNullOrWhiteSpace(feedBack.Name))
                        || (string.IsNullOrWhiteSpace(feedBack.Message) && string.IsNullOrWhiteSpace(feedBack.Subject)))
                {
                    return Ok(new { success = false, message = "Please enter email or name and subject or message" });
                }

                NormalizeFeedback(feedBack);
                feedBack.ClientData = GetClientInfo();

                string webhookUrl = _options.Value.GetSlackFeedbackWebhookUrl();
                if (string.IsNullOrWhiteSpace(webhookUrl))
                {
                    _logger.LogWarning("Slack feedback webhook URL is not configured.");
                    return Ok(new { success = false, message = "Feedback is not configured yet" });
                }

                using HttpClient client = _httpClientFactory.CreateClient();
                string messageBody = JsonSerializer.Serialize(new
                {
                    text = BuildFeedbackSlackMessage(feedBack),
                    mrkdwn = true
                });
                using var content = new StringContent(messageBody, Encoding.UTF8, "application/json");
                using HttpResponseMessage response = await client.PostAsync(webhookUrl, content);
                response.EnsureSuccessStatusCode();

                string who = string.IsNullOrWhiteSpace(feedBack.Name) || feedBack.Name == "none"
                    ? feedBack.EmailAddress
                    : feedBack.Name;

                return Ok(new { success = true, message = string.Format("Thanks for the message, {0}. We hope to get back to you soon.", who) });
            }
            catch (Exception excp)
            {
                MatrixEaseErrors.LogError(_options.Value, excp, "Error sending feedback");
                return Ok(new { success = false, message = "Failed sending feedback, please try again" });
            }
        }

        internal static string BuildFeedbackSlackMessage(Feedback feedBack)
        {
            return string.Format(
                "*MatrixEase feedback*\n*Name:* {0}\n*Email:* {1}\n*Subject:* {2}\n*Client:* {3}\n*Message:*\n{4}",
                EscapeSlackValue(feedBack.Name),
                EscapeSlackValue(feedBack.EmailAddress),
                EscapeSlackValue(feedBack.Subject),
                EscapeSlackValue(feedBack.ClientData),
                EscapeSlackValue(feedBack.Message));
        }

        private static void NormalizeFeedback(Feedback feedBack)
        {
            if (string.IsNullOrWhiteSpace(feedBack.EmailAddress))
                feedBack.EmailAddress = "none";

            if (string.IsNullOrWhiteSpace(feedBack.Name))
                feedBack.Name = "none";

            if (string.IsNullOrWhiteSpace(feedBack.Message))
                feedBack.Message = "none";

            if (string.IsNullOrWhiteSpace(feedBack.Subject))
                feedBack.Subject = "none";
        }

        private static string EscapeSlackValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "none";

            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Trim();
        }

        private string GetClientInfo()
        {
            string clientInfo = "Unknown";
            try
            {
                if (Request.HttpContext.Connection != null)
                {
                    clientInfo = string.Format("ID: {0}\r\nUser: {1}\r\n", Request.HttpContext.Connection.RemoteIpAddress, Request.HttpContext.Connection.Id);
                }
            }
            catch (Exception cexcp)
            {
                clientInfo = cexcp.Message;
            }

            return clientInfo;
        }
    }
}
