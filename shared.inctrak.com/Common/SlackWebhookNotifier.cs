using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace MatrixEase.Web.Common
{
    public static class SlackWebhookNotifier
    {
        public static string BuildPayload(string text)
        {
            return JsonSerializer.Serialize(new { text = text ?? string.Empty });
        }

        public static void Send(string webhookUrl, string text, HttpClient httpClient = null)
        {
            if (string.IsNullOrWhiteSpace(webhookUrl))
            {
                throw new InvalidOperationException("AppSettings__SlackFeedbackWebhookUrl must be configured for Slack feedback notifications.");
            }

            bool ownsClient = httpClient == null;
            HttpClient client = httpClient ?? new HttpClient();

            try
            {
                using (var content = new StringContent(BuildPayload(text), Encoding.UTF8, "application/json"))
                {
                    var response = client.PostAsync(webhookUrl, content).GetAwaiter().GetResult();
                    if (response.IsSuccessStatusCode == false)
                    {
                        string responseBody = response.Content == null
                            ? string.Empty
                            : response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        throw new InvalidOperationException(string.Format(
                            "Slack webhook rejected notification. StatusCode={0}, Body={1}",
                            (int)response.StatusCode,
                            string.IsNullOrWhiteSpace(responseBody) ? "(empty)" : responseBody));
                    }
                }
            }
            finally
            {
                if (ownsClient)
                {
                    client.Dispose();
                }
            }
        }
    }
}
