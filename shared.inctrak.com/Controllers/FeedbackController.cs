using MatrixEase.Web.Common;
using MatrixEase.Manga.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Linq;

namespace MatrixEase.Web.Controllers
{
    [ApiController]
    public class FeedbackController : ControllerBase
    {
        private readonly IOptions<AppSettings> _options;
        private readonly ILogger<FeedbackController> _logger;

        public FeedbackController(IOptions<AppSettings> options, ILogger<FeedbackController> logger)
        {
            _options = options;
            _logger = logger;
        }

        [Route("api/feedback/save_message/")]
        public ActionResult Post(Feedback feedBack)
        {
            try
            {
                StringValues header;
                Uri referrer = null;
                if (Request.Headers.TryGetValue("Referer", out header))
                    referrer = new Uri(header);

                if (referrer == null ||
                    IsAllowedReferrer(referrer) == false)
                {
                    return Ok(new { success = false, message = "Invalid request" });
                }

                if (feedBack == null || (string.IsNullOrWhiteSpace(feedBack.EmailAddress) && string.IsNullOrWhiteSpace(feedBack.Name))
                        || (string.IsNullOrWhiteSpace(feedBack.Message) && string.IsNullOrWhiteSpace(feedBack.Subject)))
                {
                    return Ok(new { success = false, message = "Please enter email or name and subject or message" });
                }
                else
                {
                    string who = "";
                    if (string.IsNullOrWhiteSpace(feedBack.EmailAddress))
                    {
                        feedBack.EmailAddress = "none";
                    }
                    else
                    {
                        who = string.Format(", {0}", feedBack.EmailAddress);
                    }
                    if (string.IsNullOrWhiteSpace(feedBack.Name))
                    {
                        feedBack.Name = "none";
                    }
                    else
                    {
                        who = string.Format(", {0}", feedBack.Name);
                    }

                    if (string.IsNullOrWhiteSpace(feedBack.Message))
                        feedBack.Message = "none";
                    else if (string.IsNullOrWhiteSpace(feedBack.Subject))
                        feedBack.Subject = "none";
                    feedBack.ClientData = GetClientInfo();

                    SlackWebhookNotifier.Send(
                        _options.Value.SlackFeedbackWebhookUrl,
                        string.Format(
                            "New feedback submitted\nSubject: {0}\nName: {1}\nEmail: {2}\nClient: {3}\nMessage: {4}",
                            feedBack.Subject,
                            feedBack.Name,
                            feedBack.EmailAddress,
                            feedBack.ClientData,
                            feedBack.Message));

                    return Ok(new { success = true, message = string.Format("Thanks for the message {0}, we hope to get back to you soon", who) });
                }
            }
            catch (Exception excp)
            {
                _logger.LogError(excp, "Error sending feedback");
                SimpleLogger.LogError(excp, "Error sending feedback");
                return Ok(new { success = false, message = "Failed sending feedback, please try again" });
            }
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

        private bool IsAllowedReferrer(Uri referrer)
        {
            foreach (string allowedOrigin in _options.Value.AllowedOrigins ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(allowedOrigin))
                {
                    continue;
                }

                if (Uri.TryCreate(allowedOrigin, UriKind.Absolute, out Uri allowedUri) == false)
                {
                    continue;
                }

                if (string.Equals(allowedUri.Scheme, referrer.Scheme, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(allowedUri.Host, referrer.Host, StringComparison.OrdinalIgnoreCase) &&
                    allowedUri.Port == referrer.Port)
                {
                    return true;
                }
            }

#if DEBUG
            return referrer.DnsSafeHost == "localhost" || referrer.DnsSafeHost == "127.0.0.1";
#else
            return false;
#endif
        }
    }
}
