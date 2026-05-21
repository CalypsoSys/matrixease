using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MatrixEase.Web.Common;
using Microsoft.Extensions.Options;
using Xunit;

namespace MatrixEase.Web.Tests;

public class HmacSupabaseTokenValidatorTests
{
    [Fact]
    public async Task ValidateTokenAsyncAcceptsValidHs256Token()
    {
        string token = CreateToken("test-secret", DateTimeOffset.UtcNow.AddMinutes(5));
        var validator = new HmacSupabaseTokenValidator(Options.Create(new AppSettings
        {
            SupabaseJwtSecret = "test-secret",
        }));

        SupabaseIdentity identity = await validator.ValidateTokenAsync(token);

        Assert.True(identity.IsAuthenticated());
        Assert.Equal("user-123", identity.ExternalIdentity);
        Assert.Equal("joe@example.com", identity.EmailAddress);
    }

    [Fact]
    public async Task ValidateTokenAsyncRejectsBadSignature()
    {
        string token = CreateToken("test-secret", DateTimeOffset.UtcNow.AddMinutes(5));
        var validator = new HmacSupabaseTokenValidator(Options.Create(new AppSettings
        {
            SupabaseJwtSecret = "different-secret",
        }));

        SupabaseIdentity identity = await validator.ValidateTokenAsync(token);

        Assert.False(identity.IsAuthenticated());
    }

    [Fact]
    public async Task ValidateTokenAsyncRejectsExpiredToken()
    {
        string token = CreateToken("test-secret", DateTimeOffset.UtcNow.AddMinutes(-5));
        var validator = new HmacSupabaseTokenValidator(Options.Create(new AppSettings
        {
            SupabaseJwtSecret = "test-secret",
        }));

        SupabaseIdentity identity = await validator.ValidateTokenAsync(token);

        Assert.False(identity.IsAuthenticated());
    }

    private static string CreateToken(string secret, DateTimeOffset expiresAt)
    {
        string header = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
        {
            alg = "HS256",
            typ = "JWT",
        }));
        string payload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
        {
            sub = "user-123",
            email = "joe@example.com",
            exp = expiresAt.ToUnixTimeSeconds(),
        }));
        string unsignedToken = $"{header}.{payload}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        string signature = Base64UrlEncode(hmac.ComputeHash(Encoding.ASCII.GetBytes(unsignedToken)));

        return $"{unsignedToken}.{signature}";
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
