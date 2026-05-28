using System;
using System.Text.Json;

namespace MatrixEase.Web.Common
{
    internal static class SupabaseJwtPayloadValidator
    {
        internal static bool Validate(JsonElement payload, AppSettings settings)
        {
            if (payload.TryGetProperty("sub", out JsonElement subElement) == false ||
                string.IsNullOrWhiteSpace(subElement.GetString()))
            {
                return false;
            }

            if (payload.TryGetProperty("exp", out JsonElement expElement) == false ||
                expElement.ValueKind != JsonValueKind.Number ||
                expElement.TryGetInt64(out long expSeconds) == false ||
                DateTimeOffset.UtcNow >= DateTimeOffset.FromUnixTimeSeconds(expSeconds))
            {
                return false;
            }

            if (HasExpectedAudience(payload, settings.GetSupabaseAudience()) == false)
            {
                return false;
            }

            string supabaseUrl = settings.GetSupabaseUrl();
            if (string.IsNullOrWhiteSpace(supabaseUrl) == false)
            {
                string expectedIssuer = $"{supabaseUrl.TrimEnd('/')}/auth/v1";
                if (payload.TryGetProperty("iss", out JsonElement issuerElement) == false ||
                    string.Equals(issuerElement.GetString(), expectedIssuer, StringComparison.Ordinal) == false)
                {
                    return false;
                }
            }

            return true;
        }

        internal static SupabaseIdentity BuildIdentity(JsonElement payload)
        {
            return new SupabaseIdentity
            {
                ExternalIdentity = payload.GetProperty("sub").GetString(),
                EmailAddress = payload.TryGetProperty("email", out JsonElement emailElement) ? emailElement.GetString() : null
            };
        }

        private static bool HasExpectedAudience(JsonElement payload, string expectedAudience)
        {
            if (string.IsNullOrWhiteSpace(expectedAudience))
            {
                return true;
            }

            if (payload.TryGetProperty("aud", out JsonElement audienceElement) == false)
            {
                return false;
            }

            if (audienceElement.ValueKind == JsonValueKind.String)
            {
                return string.Equals(audienceElement.GetString(), expectedAudience, StringComparison.Ordinal);
            }

            if (audienceElement.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement audience in audienceElement.EnumerateArray())
                {
                    if (audience.ValueKind == JsonValueKind.String &&
                        string.Equals(audience.GetString(), expectedAudience, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
