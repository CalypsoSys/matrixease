using MatrixEase.Manga.com.Common;
using MatrixEase.Manga.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace MatrixEase.Manga.com.Controllers
{
    [ApiController]
    public class FeedbackController : ControllerBase
    {
        private readonly IOptions<AppSettings> _options;
        private readonly ILogger<DefaultController> _logger;

        public FeedbackController(IOptions<AppSettings> options, ILogger<DefaultController> logger)
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
                    (referrer.DnsSafeHost.EndsWith("matrixease.com") == false
#if DEBUG 
                    && referrer.DnsSafeHost != "localhost"
#endif
                    ))
                {
                    return Ok(new { success = false, message = "Invalid request" });
                }
                var incTrak = referrer.GetLeftPart(UriPartial.Authority);

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

                    var subject = string.Format("Feedback: {0}", feedBack.Subject);
                    var messageBody = string.Format("Name: {0}<br />Email: {1}<br />Client:{2}<br />Message: {3}", feedBack.Name, feedBack.EmailAddress, feedBack.ClientData, feedBack.Message);

                    if (_options.Value.UseSNMP)
                    {
                        var client = new SmtpClient(_options.Value.GetSNMPServer(), _options.Value.SNMPPort);
                        client.UseDefaultCredentials = false;
                        client.Credentials = new NetworkCredential(_options.Value.GetSNMPAddress(), _options.Value.GetSNMPPassword());
                        client.EnableSsl = false;
                        client.DeliveryMethod = SmtpDeliveryMethod.Network;

                        MailMessage message = new MailMessage(_options.Value.GetSNMPAddress(), "feedback@matrixease.com");
                        message.Subject = subject;
                        message.Body = messageBody;
                        message.IsBodyHtml = true;
                        message.Bcc.Add(_options.Value.GetSNMPAddress());
                        client.Send(message);
                    }
                    else
                    {
                        var client = new SendGridClient(_options.Value.GetEmailApiKey());
                        var from = new EmailAddress(_options.Value.GetEmailFrom());
                        var to = new EmailAddress("feedback@matrixease.com");
                        var msg = MailHelper.CreateSingleEmail(from, to, subject, messageBody, null);

                        var response = client.SendEmailAsync(msg);
                        response.Wait();
                    }

                    return Ok(new { success = true, message = string.Format("Thanks for the message {0}, we hope to get back to you soon", who) });
                }
            }
            catch (Exception excp)
            {
                MyLogger.LogError(excp, "Error sending feedback");
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

    }
}
