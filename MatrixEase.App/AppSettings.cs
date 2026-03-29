using MatrixEase.Manga.Utility;
namespace Desktop.MatrixEase.Manga
{
    public class AppSettings
    {
        public string GoogleClientId { get; set; }
        public string GoogleClientSecret { get; set; }

        public string GetGoogleClientId()
        {
            return GoogleClientId;
        }

        public string GetGoogleClientSecret()
        {
            return GoogleClientSecret;
        }
    }
}
