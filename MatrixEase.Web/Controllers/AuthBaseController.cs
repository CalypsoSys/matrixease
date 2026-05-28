using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatrixEase.Manga.Utility;
using Microsoft.AspNetCore.Mvc;

namespace MatrixEase.Web
{
    public class AuthBaseController : ControllerBase
    {
        private const string MatrixIdPurpose = "MatrixEase.MatrixId";

        protected string Encode(string userFolder, Guid mangaGuid)
        {
            try
            {
                List<byte> plainBytes = new List<byte>();
                plainBytes.AddRange(mangaGuid.ToByteArray());
                plainBytes.AddRange(Encoding.UTF8.GetBytes(userFolder));
                return SecretProtector.Protect(plainBytes.ToArray(), MatrixIdPurpose);
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "Error encoding MatrixEase ID");
                throw;
            }
        }

        protected Tuple<string, Guid> Decrypt(string mxesId)
        {
            byte[] plainTextBytes = SecretProtector.Unprotect(mxesId, MatrixIdPurpose);
            Guid mangaGuid = new Guid(plainTextBytes.Take(16).ToArray());
            string userFolder = Encoding.UTF8.GetString(plainTextBytes.Skip(16).ToArray());
            return Tuple.Create(userFolder, mangaGuid);
        }
    }
}
