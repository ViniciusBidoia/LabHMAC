using LabHMAC.Api.Application;
using LabHMAC.Api.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LabHMAC.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="HmacService"/>. Each test name describes the scenario in plain English.
/// </summary>
public class HmacServiceTests
{
    private const string TestSecretKey = "test-secret-key";

    private static HmacService CreateService(string secretKey = TestSecretKey)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HMAC:SecretKey"] = secretKey
            })
            .Build();

        return new HmacService(config, NullLogger<HmacService>.Instance);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyOrWhitespaceSecretKey_ThrowsInvalidOperationException(string secretKey)
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => CreateService(secretKey));
        Assert.Contains("cannot be empty or whitespace", ex.Message);
    }

    // ──────────────────────────────────────────────────
    // T011 [US1] — ComputeSignature returns correct hex
    // ──────────────────────────────────────────────────

    [Fact]
    public void ComputeSignature_ReturnsLowercase64CharHexString()
    {
        var service = CreateService();
        string signature = service.ComputeSignature("{\"amount\":10}");

        Assert.Equal(64, signature.Length);
        Assert.True(signature.All(c => char.IsAsciiHexDigit(c)), "Signature should be hex characters only.");
        Assert.Equal(signature, signature.ToLowerInvariant()); // must be lowercase
    }

    [Fact]
    public void ComputeSignature_SamePayloadAndKey_ProducesSameSignature()
    {
        var service = CreateService();
        string sig1 = service.ComputeSignature("{\"amount\":10}");
        string sig2 = service.ComputeSignature("{\"amount\":10}");

        Assert.Equal(sig1, sig2);
    }

    [Fact]
    public void ComputeSignature_DifferentPayloads_ProduceDifferentSignatures()
    {
        var service = CreateService();
        string sig1 = service.ComputeSignature("{\"amount\":10}");
        string sig2 = service.ComputeSignature("{\"amount\":20}");

        Assert.NotEqual(sig1, sig2);
    }

    // ──────────────────────────────────────────────────
    // T011 [US1] — Validate with matching signature
    // ──────────────────────────────────────────────────

    [Fact]
    public void Validate_MatchingSignature_ReturnsValid()
    {
        var service = CreateService();
        string payload = "{\"transactionId\":\"abc\",\"amount\":49.90}";
        string signature = service.ComputeSignature(payload);

        HmacValidationResult result = service.Validate(payload, signature);

        Assert.True(result.IsValid);
        Assert.Equal("Request integrity verified.", result.Message);
    }

    // ──────────────────────────────────────────────────
    // T016 [US2] — Validate with tampered body
    // ──────────────────────────────────────────────────

    [Fact]
    public void Validate_TamperedBody_ReturnsInvalid()
    {
        var service = CreateService();
        string originalPayload = "{\"amount\":49.90}";
        string tamperedPayload = "{\"amount\":99.99}";
        string signature = service.ComputeSignature(originalPayload);

        HmacValidationResult result = service.Validate(tamperedPayload, signature);

        Assert.False(result.IsValid);
        Assert.Equal("Signature mismatch. Request integrity compromised.", result.Message);
        Assert.NotNull(result.ReceivedSignature);
        Assert.NotNull(result.ComputedSignature);
        Assert.NotEqual(result.ReceivedSignature, result.ComputedSignature);
    }

    // ──────────────────────────────────────────────────
    // T018 [US3] — Validate with null/empty signature
    // ──────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_NullOrEmptySignature_ReturnsMissingHeader(string? signature)
    {
        var service = CreateService();

        HmacValidationResult result = service.Validate("{\"amount\":10}", signature);

        Assert.False(result.IsValid);
        Assert.Equal("X-Hmac-Signature header is missing.", result.Message);
    }

    // ──────────────────────────────────────────────────
    // T019 [US3] — Validate with malformed hex signature
    // ──────────────────────────────────────────────────

    [Theory]
    [InlineData("not-a-hex-string-at-all")]
    [InlineData("ZZZZ0000111122223333444455556666777788889999aaaabbbbccccddddeee")]
    [InlineData("abcdef")] // too short
    public void Validate_MalformedHexSignature_ReturnsInvalidWithFormatMessage(string signature)
    {
        var service = CreateService();

        HmacValidationResult result = service.Validate("{\"amount\":10}", signature);

        Assert.False(result.IsValid);
        Assert.Contains("not a valid hex string", result.Message);
    }
}
