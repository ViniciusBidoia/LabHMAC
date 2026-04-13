using LabHMAC.Api.Domain;

namespace LabHMAC.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="HmacValidationResult"/> factory methods.
/// </summary>
public class HmacValidationResultTests
{
    // ──────────────────────────────────────────────────
    // T012 [US1] — Factory method tests
    // ──────────────────────────────────────────────────

    [Fact]
    public void Valid_SetsIsValidTrue_AndCorrectMessage()
    {
        var result = HmacValidationResult.Valid("abcd1234");

        Assert.True(result.IsValid);
        Assert.Equal("Request integrity verified.", result.Message);
        Assert.Equal("abcd1234", result.ComputedSignature);
        Assert.Equal("abcd1234", result.ReceivedSignature);
    }

    [Fact]
    public void Invalid_SetsIsValidFalse_AndPreservesReason()
    {
        var result = HmacValidationResult.Invalid("Signature mismatch.", received: "aaa", computed: "bbb");

        Assert.False(result.IsValid);
        Assert.Equal("Signature mismatch.", result.Message);
        Assert.Equal("aaa", result.ReceivedSignature);
        Assert.Equal("bbb", result.ComputedSignature);
    }

    [Fact]
    public void MissingHeader_SetsIsValidFalse_AndCorrectMessage()
    {
        var result = HmacValidationResult.MissingHeader();

        Assert.False(result.IsValid);
        Assert.Equal("X-Hmac-Signature header is missing.", result.Message);
        Assert.Null(result.ReceivedSignature);
        Assert.Null(result.ComputedSignature);
    }
}
