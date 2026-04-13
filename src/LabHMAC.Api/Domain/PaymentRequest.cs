namespace LabHMAC.Api.Domain;

/// <summary>
/// Represents the payment payload sent by a terminal (acquirer) to the backend.
/// This entity is the <em>message</em> whose integrity is protected by the HMAC signature.
/// </summary>
/// <remarks>
/// <b>HMAC context</b>: The raw JSON bytes of this object are what the HMAC is computed over.
/// The terminal serializes this to JSON, computes HMAC-SHA256(secret, jsonBytes), and sends
/// both the JSON body and the resulting signature in the <c>X-Hmac-Signature</c> header.
/// </remarks>
public sealed class PaymentRequest
{
    /// <summary>Unique identifier for the transaction.</summary>
    public Guid TransactionId { get; init; }

    /// <summary>Transaction amount in the merchant's currency (must be greater than zero).</summary>
    public decimal Amount { get; init; }

    /// <summary>Identifies the merchant / store originating the transaction.</summary>
    public string MerchantId { get; init; } = string.Empty;

    /// <summary>Timestamp from the terminal's clock (ISO-8601).</summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Validates that all required fields are populated and business rules are met.
    /// Keeps validation logic inside the domain object (DDD: rich entity).
    /// </summary>
    /// <returns><c>true</c> if the request is structurally valid; otherwise <c>false</c>.</returns>
    public bool IsValid() =>
        TransactionId != Guid.Empty
        && Amount > 0
        && !string.IsNullOrWhiteSpace(MerchantId);
}
