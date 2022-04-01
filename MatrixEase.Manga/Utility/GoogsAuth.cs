using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Utility
{
    public static class GoogsAuth
    {
        public const string GoogsJson = @"{'installed': {'client_id': '#PARAM1#','project_id':'matrixease','auth_uri': 'https://accounts.google.com/o/oauth2/auth','token_uri':'https://oauth2.googleapis.com/token','auth_provider_x509_cert_url':'https://www.googleapis.com/oauth2/v1/certs','client_secret':'#PARAM2#','redirect_uris':['urn:ietf:wg:oauth:2.0:oob','http://localhost']}}";

        public static UserCredential AuthenticateLocal(string googleClientId, string googleClientSecret)
        {
            UserCredential credential;
            string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };
            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream))
                {
                    var clientSecret = GoogsAuth.GoogsJson.Replace("{", "{{").Replace("}", "}}").Replace("#PARAM1#", "{0}").Replace("#PARAM2#", "{1}").Replace("'", "\"");
                    writer.Write(string.Format(clientSecret, googleClientId, googleClientSecret));
                    writer.Flush();
                    stream.Position = 0;
                    // The file token.json stores the user's access and refresh tokens, and is created
                    // automatically when the authorization flow completes for the first time.
                    string credPath = MiscHelpers.GetGoogsFile("token.json");
                    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(credPath, true)).Result;
                }
            }

            return credential;
        }
    }
}
