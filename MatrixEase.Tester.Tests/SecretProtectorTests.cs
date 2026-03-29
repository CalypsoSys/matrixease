using System;
using System.Security.Cryptography;
using MatrixEase.Manga.Utility;
using Xunit;

namespace MatrixEase.Tester.Tests;

public class SecretProtectorTests
{
    [Fact]
    public void ProtectAndUnprotectString_RoundTrips()
    {
        SecretProtector.Configure("test-protection-key-0123456789");

        string protectedValue = SecretProtector.ProtectString("hello world", "MatrixEase.Test");

        Assert.Equal("hello world", SecretProtector.UnprotectString(protectedValue, "MatrixEase.Test"));
    }

    [Fact]
    public void Unprotect_WithTamperedPayload_Throws()
    {
        SecretProtector.Configure("test-protection-key-0123456789");

        string protectedValue = SecretProtector.ProtectString("hello world", "MatrixEase.Test");
        int tamperIndex = protectedValue.Length / 2;
        char replacement = protectedValue[tamperIndex] == 'A' ? 'B' : 'A';
        string tamperedValue = protectedValue[..tamperIndex] + replacement + protectedValue[(tamperIndex + 1)..];

        Assert.ThrowsAny<CryptographicException>(() => SecretProtector.UnprotectString(tamperedValue, "MatrixEase.Test"));
    }

    [Fact]
    public void Unprotect_WithWrongPurpose_Throws()
    {
        SecretProtector.Configure("test-protection-key-0123456789");

        string protectedValue = SecretProtector.ProtectString("hello world", "MatrixEase.Test.One");

        Assert.ThrowsAny<CryptographicException>(() => SecretProtector.UnprotectString(protectedValue, "MatrixEase.Test.Two"));
    }
}
