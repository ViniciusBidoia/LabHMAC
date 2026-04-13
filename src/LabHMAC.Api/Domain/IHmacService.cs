namespace LabHMAC.Api.Domain;

/// <summary>
/// Domain service contract for HMAC SHA-256 signing and verification.
/// </summary>
/// <remarks>
/// <b>Why an interface in the Domain layer?</b> Following the Dependency Inversion Principle (DIP),
/// the controller and filters depend on this abstraction — not on the concrete cryptographic
/// implementation. This makes unit testing trivial (mock this interface) and keeps the domain
/// layer free of infrastructure concerns.
/// </remarks>
public interface IHmacService
{
    /// <summary>
    /// Computes the HMAC SHA-256 signature of the given payload using the configured secret key.
    /// </summary>
    /// <param name="payload">The raw request body string (UTF-8).</param>
    /// <returns>Lowercase hex string (64 characters) representing the 32-byte HMAC hash.</returns>
    string ComputeSignature(string payload);

    /// <summary>
    /// Computes the server-side signature and compares it to the received signature
    /// using a timing-safe comparison to prevent side-channel attacks.
    /// </summary>
    /// <param name="payload">The raw request body string (UTF-8).</param>
    /// <param name="receivedSignature">
    /// The value of the <c>X-Hmac-Signature</c> header, or <c>null</c> if the header was missing.
    /// </param>
    /// <returns>An <see cref="HmacValidationResult"/> describing the outcome.</returns>
    HmacValidationResult Validate(string payload, string? receivedSignature);
}
