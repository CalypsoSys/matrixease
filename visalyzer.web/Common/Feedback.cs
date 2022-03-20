using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MatrixEase.Manga.com.Common
{
    public partial class Feedback
    {
        public string EmailAddress { get; set; }
        public string Name { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public string ClientData { get; set; }
    }
}
