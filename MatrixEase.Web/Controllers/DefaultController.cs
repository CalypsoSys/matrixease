using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using MatrixEase.Manga.Manga;
using MatrixEase.Manga.Processing;
using MatrixEase.Manga.Utility;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace MatrixEase.Manga.com
{
    [Route("/")]
    public class DefaultController : AuthBaseController
    {
        private readonly IOptions<AppSettings> _options;
        private readonly ILogger<DefaultController> _logger;

        public DefaultController(IOptions<AppSettings> options, ILogger<DefaultController> logger)
        {
            _options = options;
            _logger = logger;
        }

        [HttpGet]
        public RedirectResult Get()
        {
            return Redirect(new Uri("/index.html", UriKind.Relative).ToString());
        }

        [HttpGet("/js/vis_init.js")]
        public string MatrixEaseIdDebug()
        {
            return MatrixEaseId();
        }

        [HttpGet("/js/vis_init.min.js")]
        public string MatrixEaseIdRelease()
        {
            return MatrixEaseId();
        }

        private string MatrixEaseId()
        {
            CookieOptions option = new CookieOptions();
            option.Expires = DateTime.Now.AddMinutes(30);
            string matrixease_id = MiscHelpers.Encrypt(Guid.NewGuid().ToString());
            Response.Cookies.Append("authenticated-accepted-3", matrixease_id, option);

            return string.Format("document.write('<form><input type=\"hidden\" id=\"matrixease_id\" name=\"matrixease_id\" value=\"{0}\"/></form>');", matrixease_id);
        }

        [HttpGet("get_access")]
        public object GetAccessToken(string matrixease_id, string access_token)
        {
            try
            {
                CheckMatrixEaseId(matrixease_id, true);
                var tok = Decrypt(access_token);
                if (MangaState.ValidateUserMangaCatalog(tok) != MangaAuthType.Invalid)
                {
                    return new { Success = true, AccessToken = access_token };
                }
            }
            catch(Exception excp)
            {
                MyLogger.LogError(excp, "Error getting access token");
            }
            return new { Success = false, AccessToken = "" };
        }

        [HttpGet("captcha")]
        public object Captcha(string matrixease_id)
        {
            try
            {
                CheckMatrixEaseId(matrixease_id, true);
                Random rnd = new Random();
                int num1 = rnd.Next(1, 9);
                int num2 = rnd.Next(1, 9);

                CookieOptions option = new CookieOptions();
                option.Expires = DateTime.Now.AddSeconds(35);
                string coded = MiscHelpers.Encrypt(string.Format("{0},{1},{2}", num1, num2, num1 + num2));
                Response.Cookies.Append("authenticated-accepted-2", coded, option);

                return new { num1 = num1, num2 = num2 };
            }
            catch (Exception excp)
            {
                MyLogger.LogError(excp, "Error getting captcha");
            }
            return new { num1 = 0, num2 = 0 };
        }

        //curl https://localhost:44340/send_email_code/?email_to_address=bob
        [HttpGet("send_email_code")]
        public object SendEmailCode(string matrixease_id, string email_to_address, string result)
        {
            bool status = false;
            try
            {
                CheckMatrixEaseId(matrixease_id, true);
                string coded;
                if (MiscHelpers.IsValidEmail(email_to_address) && Request.Cookies.TryGetValue("authenticated-accepted-2", out coded))
                {
                    string captcha = MiscHelpers.Decrypt(coded, false);
                    string []parts1 = captcha.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    string[] parts2 = result.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (parts1.Length == 3 && parts1.Length == parts2.Length && parts1[0] == parts2[0] && parts1[1] == parts2[1] && parts1[2] == parts2[2])
                    {
                        Random rnd = new Random();
                        int rndCode = rnd.Next(0, 1000000);
                        string code = rndCode.ToString("000000");

                        string filePath = MiscHelpers.GetPendingAccountFile(email_to_address);
                        using (new FileLocker(filePath))
                        {
                            using (FileStream pendingStream = new FileStream(filePath, FileMode.OpenOrCreate))
                            {
                                using (BinaryWriter pending = new BinaryWriter(pendingStream))
                                {

                                    pending.Write(MiscHelpers.Encrypt(email_to_address.ToLower(), code));
                                    pending.Write(code);
                                    pending.Write(MiscHelpers.Encrypt(MiscHelpers.HashEmail(email_to_address, false), code));
                                }
                            }
                        }

                        var plainTextContent = "Here's your email validation code. To complete the process, either enter or copy and paste the six digits of the code into the MatrixEase access page and click \"Validate Code\" to continue. That's it!";
                        if (_options.Value.UseSNMP)
                        {
                            var client = new SmtpClient(_options.Value.GetSNMPServer(), _options.Value.SNMPPort);
                            client.UseDefaultCredentials = false;
                            client.Credentials = new NetworkCredential(_options.Value.GetSNMPAddress(), _options.Value.GetSNMPPassword());
                            client.EnableSsl = false;
                            client.DeliveryMethod = SmtpDeliveryMethod.Network;

                            MailMessage message = new MailMessage(_options.Value.GetSNMPAddress(), email_to_address);
                            message.Subject = "MatrixEase Validation Code";
                            message.Body = plainTextContent;
                            message.IsBodyHtml = false;
                            message.Bcc.Add(_options.Value.GetSNMPAddress());
                            client.Send(message);
                        }
                        else
                        {
                            var client = new SendGridClient(_options.Value.GetEmailApiKey());
                            var from = new EmailAddress(_options.Value.GetEmailFrom());
                            var to = new EmailAddress(email_to_address);
                            var msg = MailHelper.CreateSingleEmail(from, to, "MatrixEase Validation Code", string.Format("{0}\r\n\r\n{1}\r\n\r\nBest Regards,\r\nThe MatrixEase Team", plainTextContent, code), null);

                            var response = client.SendEmailAsync(msg);
                            response.Wait();
                            status = true;
                        }
                    }
                }
            }
            catch (Exception excp)
            {
                MyLogger.LogError(excp, "Error sending email code {0}", email_to_address);
            }

            return new { Success = status };
        }

        [HttpGet("validate_email_code")]
        public object ValidateEmailCode(string matrixease_id, string email_to_address, string emailed_code)
        {
            bool status = false;
            try
            {
                CheckMatrixEaseId(matrixease_id, true);
                if (MiscHelpers.IsValidEmail(email_to_address) )
                {
                    string filePath = MiscHelpers.GetPendingAccountFile(email_to_address);
                    if (System.IO.File.Exists(filePath))
                    {
                        using (new FileLocker(filePath))
                        {
                            using (FileStream pendingStream = new FileStream(filePath, FileMode.Open))
                            {
                                using (BinaryReader pending = new BinaryReader(pendingStream))
                                {
                                    string emailAddress = MiscHelpers.Decrypt(pending.ReadString(), emailed_code);
                                    string code = pending.ReadString();
                                    string emailAddressHash = MiscHelpers.Decrypt(pending.ReadString(), emailed_code);
                                    if (emailAddress.ToLower() == email_to_address.ToLower() && code == emailed_code && emailAddressHash == MiscHelpers.HashEmail(email_to_address, false))
                                    {
                                        CookieOptions option = new CookieOptions();
                                        option.Expires = DateTime.Now.AddMinutes(30);
                                        string visEmailClaimId = MiscHelpers.Encrypt(emailAddressHash);
                                        Response.Cookies.Append("VisEmailClaimId", visEmailClaimId, option);

                                        SetAccess(emailAddressHash, email_to_address, MangaAuthType.Email);
                                        status = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception excp)
            {
                MyLogger.LogError(excp, "Error validating email code for {0}", email_to_address);
            }

            return new { Success = status };
        }

        [HttpGet("my_mangas")]
        public object MyMangas(string matrixease_id)
        {
            try
            {
                CheckMatrixEaseId(matrixease_id, false);
                var myIds = GetMyIdentities(false);
                MangaAuthType auth = ValidateAccess(null, myIds, false);
                if (auth != MangaAuthType.Invalid)
                {
                    string userId = MyIdentity(myIds, auth);

                    MangaCatalog cats = MangaState.LoadUserMangaCatalog(userId, new MangaLoadOptions(true));

                    List<object> mangas = new List<object>();
                    List<Guid> loadedMangas = new List<Guid>();
                    foreach(var manga in cats.MyMangas)
                    {
                        var visId = Encode(userId, manga.ManagGuid);
                        mangas.Add(new {Name= manga.MangaName, 
                            Url= new Uri(string.Format("/visualize.html?matrixease_id={0}&vis_id={1}", HttpUtility.UrlEncode(matrixease_id), HttpUtility.UrlEncode(visId)), UriKind.Relative).ToString(),
                            Original = manga.OriginalName, Type = manga.SheetType, Created =manga.Created, MaxRows = manga.MaxRows,
                            TotalRows = manga.TotalRows, Status = manga.Status});
                        loadedMangas.Add(manga.ManagGuid);
                    }
                    foreach(var manga in MangaFactory.GetPending(userId))
                    {
                        if (loadedMangas.Contains(manga.ManagGuid) == false)
                        {
                            mangas.Add(new
                            {
                                Name = manga.MangaName,
                                Url = "#",
                                Original = manga.OriginalName,
                                Type = manga.SheetType,
                                Created = manga.Created,
                                MaxRows = manga.MaxRows,
                                TotalRows = "N/A",
                                Status = manga.Status
                            });
                        }

                    }
                    return new { Success = true, MyMangas = mangas };
                }
            }
            catch (Exception excp)
            {
                MyLogger.LogError(excp, "Error getting my mangas");
            }

            return new { Success = false };
        }
    }
}
