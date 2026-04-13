namespace LabHMAC.Api.Domain;

/// <summary>
/// Immutable value object that encapsulates the outcome of an HMAC validation step.
/// Created exclusively via static factory methods to enforce correct construction.
/// </summary>
/// <remarks>
/// <b>Why a value object?</b> The validation result has no identity — two results with the
/// same field values are semantically equal. Making it immutable prevents accidental mutation
/// after the cryptographic check is complete.
/// </remarks>
public sealed class HmacValidationResult
{
    /// <summary><c>true</c> when the received signature matches the server-computed HMAC.</summary>
    public bool IsValid { get; private init; }

    /// <summary>Human-readable outcome message, also returned in the HTTP response body.</summary>
    public string Message { get; private init; } = string.Empty;

    /// <summary>Hex string received in the <c>X-Hmac-Signature</c> header; <c>null</c> if the header was missing.</summary>
    public string? ReceivedSignature { get; private init; }

    /// <summary>Server-computed hex string; populated for diagnostic logging.</summary>
    public string? ComputedSignature { get; private init; }

    private HmacValidationResult() { }

    /// <summary>
    /// Factory: the request is integral — signatures match.
    /// </summary>
    /// <param name="computedSignature">The hex signature computed by the server.</param>
    public static HmacValidationResult Valid(string computedSignature) => new()
    {
        IsValid = true,
        Message = "Request integrity verified.",
        ReceivedSignature = computedSignature,
        ComputedSignature = computedSignature
    };

    /// <summary>
    /// Factory: the request failed validation (tampered body, wrong key, bad format, etc.).
    /// </summary>
    /// <param name="reason">Human-readable explanation of why validation failed.</param>
    /// <param name="received">The signature received from the client (may be <c>null</c>).</param>
    /// <param name="computed">The server-computed signature (may be <c>null</c>).</param>
    public static HmacValidationResult Invalid(string reason, string? received = null, string? computed = null) => new()
    {
        IsValid = false,
        Message = reason,
        ReceivedSignature = received,
        ComputedSignature = computed
    };

    /// <summary>
    /// Factory: the <c>X-Hmac-Signature</c> header was not present in the request.
    /// </summary>
    public static HmacValidationResult MissingHeader() => new()
    {
        IsValid = false,
        Message = "X-Hmac-Signature header is missing.",
        ReceivedSignature = null,
        ComputedSignature = null
    };
}
