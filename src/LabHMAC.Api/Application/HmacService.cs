using System.Security.Cryptography;
using System.Text;
using LabHMAC.Api.Domain;

namespace LabHMAC.Api.Application;

/// <summary>
/// Concrete HMAC SHA-256 signing and verification service.
/// Each method isolates one step of the algorithm so learners can follow the flow.
/// </summary>
/// <remarks>
/// <b>Algorithm overview</b> (each step maps to a named method below):
/// <list type="number">
///   <item>Encode the shared secret key as UTF-8 bytes (<see cref="GetKeyBytes"/>).</item>
///   <item>Encode the request body (payload) as UTF-8 bytes (<see cref="GetPayloadBytes"/>).</item>
///   <item>Compute HMAC-SHA256(keyBytes, payloadBytes) using <see cref="HMACSHA256.HashData"/>.</item>
///   <item>Convert the 32-byte hash to a lowercase hex string (<see cref="ToHexString"/>).</item>
///   <item>Compare using <see cref="CryptographicOperations.FixedTimeEquals"/> — timing-safe.</item>
/// </list>
/// </remarks>
public sealed class HmacService : IHmacService
{
    private readonly string _secretKey;
    private readonly ILogger<HmacService> _logger;

    public HmacService(IConfiguration configuration, ILogger<HmacService> logger)
    {
        // FR-004: Key is loaded from configuration, never hardcoded.
        _secretKey = configuration["HMAC:SecretKey"]
            ?? throw new InvalidOperationException("HMAC:SecretKey is not configured. Set it in appsettings.json or via the HMAC__SecretKey environment variable.");

        if (string.IsNullOrWhiteSpace(_secretKey))
        {
            throw new InvalidOperationException("HMAC:SecretKey cannot be empty or whitespace. Set a strong shared secret in configuration.");
        }

        _logger = logger;
    }

    /// <inheritdoc />
    public string ComputeSignature(string payload)
    {
        // Step 1: Encode the shared secret key as UTF-8 bytes.
        byte[] keyBytes = GetKeyBytes();
        _logger.LogDebug("HMAC Step 1 — Secret key loaded and encoded to {KeyLength} bytes.", keyBytes.Length);

        // Step 2: Encode the request body as UTF-8 bytes.
        byte[] payloadBytes = GetPayloadBytes(payload);
        _logger.LogDebug("HMAC Step 2 — Payload encoded to {PayloadLength} bytes.", payloadBytes.Length);

        // Step 3: Compute HMAC-SHA256 in a single BCL call.
        byte[] hash = HMACSHA256.HashData(keyBytes, payloadBytes);
        _logger.LogDebug("HMAC Step 3 — HMAC-SHA256 computed ({HashLength} bytes).", hash.Length);

        // Step 4: Convert the hash to a lowercase hex string.
        string hexSignature = ToHexString(hash);
        _logger.LogDebug("HMAC Step 4 — Hex signature: {Signature}", hexSignature);

        return hexSignature;
    }

    /// <inheritdoc />
    public HmacValidationResult Validate(string payload, string? receivedSignature)
    {
        // Guard: header missing?
        if (string.IsNullOrEmpty(receivedSignature))
        {
            _logger.LogDebug("HMAC Validation — X-Hmac-Signature header is missing.");
            return HmacValidationResult.MissingHeader();
        }

        // Guard: is the received signature a valid 64-character hex string?
        if (!IsValidHexSignature(receivedSignature))
        {
            _logger.LogDebug("HMAC Validation — Received signature is not valid hex: {Received}", receivedSignature);
            return HmacValidationResult.Invalid(
                "X-Hmac-Signature header value is not a valid hex string. Expected 64 lowercase hex characters.",
                received: receivedSignature,
                computed: null);
        }

        // Compute the server-side signature.
        string computedSignature = ComputeSignature(payload);

        // Step 5: Timing-safe comparison using CryptographicOperations.FixedTimeEquals.
        // Why timing-safe? A regular string comparison short-circuits on the first different byte,
        // leaking timing information that an attacker could exploit to reconstruct the correct
        // signature one byte at a time.
        byte[] receivedBytes = Convert.FromHexString(receivedSignature);
        byte[] computedBytes = Convert.FromHexString(computedSignature);

        bool signaturesMatch = CryptographicOperations.FixedTimeEquals(receivedBytes, computedBytes);
        _logger.LogDebug(
            "HMAC Step 5 — Timing-safe comparison result: {Match}. Received: {Received}, Computed: {Computed}",
            signaturesMatch, receivedSignature, computedSignature);

        return signaturesMatch
            ? HmacValidationResult.Valid(computedSignature)
            : HmacValidationResult.Invalid(
                "Signature mismatch. Request integrity compromised.",
                received: receivedSignature,
                computed: computedSignature);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Private helpers — each isolates one algorithm step for readability.
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>Encodes the configured secret key as UTF-8 bytes.</summary>
    private byte[] GetKeyBytes() => Encoding.UTF8.GetBytes(_secretKey);

    /// <summary>Encodes the payload string as UTF-8 bytes.</summary>
    private static byte[] GetPayloadBytes(string payload) => Encoding.UTF8.GetBytes(payload);

    /// <summary>Converts a byte array to a lowercase hex string.</summary>
    private static string ToHexString(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();

    /// <summary>
    /// Validates that the signature is a 64-character lowercase hex string.
    /// HMAC-SHA256 produces 32 bytes = 64 hex characters.
    /// </summary>
    private static bool IsValidHexSignature(string signature)
    {
        if (signature.Length != 64)
            return false;

        foreach (char c in signature)
        {
            if (!char.IsAsciiHexDigit(c))
                return false;
        }

        return true;
    }
}
