# Data Model: HMAC SHA-256 Request Validation Lab

**Feature**: `001-hmac-sha256-request-validation`  
**Date**: 2026-04-12

---

## Domain Objects

### Entity: `PaymentRequest`

Represents the payload that a payment terminal (acquirer) sends to the backend.
This entity is the *message* being protected by the HMAC signature.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `TransactionId` | `Guid` | Required, non-empty | Unique identifier for the transaction |
| `Amount` | `decimal` | Required, > 0 | Transaction amount; e.g., `49.90` |
| `MerchantId` | `string` | Required, non-empty | Identifies the merchant/store |
| `Timestamp` | `DateTimeOffset` | Required | ISO-8601 datetime; terminal's clock |

**Behaviour**:
- `IsValid()` → validates that all required fields are populated and `Amount > 0`.
  Keeps validation logic inside the domain object (DDD: rich entity).

**C# namespace**: `LabHMAC.Api.Domain`  
**Notes**: This object is deserialized from the raw JSON body.  
The same raw body bytes are what the HMAC is computed over — serialize/deserialize order matters.

---

### Value Object: `HmacValidationResult`

Immutable result returned by `IHmacService.Validate(...)`.
Encapsulates the outcome of the HMAC verification step.

| Field | Type | Notes |
|-------|------|-------|
| `IsValid` | `bool` | `true` = signatures match and request is integral |
| `Message` | `string` | Human-readable outcome message (also returned to caller) |
| `ReceivedSignature` | `string?` | Hex string from `X-Hmac-Signature` header; `null` if missing |
| `ComputedSignature` | `string?` | Server-computed hex string; populated for diagnostic logging |

**Behaviour**:
- `static Valid(string computedSig)` → factory method for successful validation.
- `static Invalid(string reason, string? received, string? computed)` → factory for failures.
- `static MissingHeader()` → factory for missing-header scenario.

**Immutability**: All properties are `init`-only. Created via static factory methods only.

**C# namespace**: `LabHMAC.Api.Domain`

---

### Domain Service Interface: `IHmacService`

Defines the contract for HMAC signing and verification.
Placing this interface in the Domain layer follows DIP and allows the controller
to depend on an abstraction rather than the concrete cryptographic implementation.

```
IHmacService
  + ComputeSignature(string payload) : string
      // Computes HMAC SHA-256 of payload using configured key.
      // Returns lowercase hex string of the 32-byte hash.

  + Validate(string payload, string? receivedSignature) : HmacValidationResult
      // Computes server-side signature and compares to receivedSignature.
      // Uses CryptographicOperations.FixedTimeEquals for timing-safe comparison.
```

**C# namespace**: `LabHMAC.Api.Domain`

---

### Application Service: `HmacService` (implements `IHmacService`)

Concrete implementation of HMAC SHA-256 signing and verification.
Lives in the Application layer (infrastructure concern: cryptography).

**Dependencies**: `IConfiguration` (to load `HMAC:SecretKey`), `ILogger<HmacService>`

**Key implementation notes** (documented in code):
1. Key is UTF-8 encoded on every call (stateless; key is a string from config).
2. Payload is UTF-8 encoded — same encoding as terminal must use.
3. `HMACSHA256.HashData(keyBytes, messageBytes)` performs the HMAC in one BCL call.
4. Hash is converted to lowercase hex: `Convert.ToHexString(hash).ToLower()`.
5. Comparison uses `CryptographicOperations.FixedTimeEquals` — timing-safe.

**C# namespace**: `LabHMAC.Api.Application`

---

## State Transitions

```
Request received
       │
       ▼
[X-Hmac-Signature header present?]
   No  ──────────────────────────────► HmacValidationResult.MissingHeader()
                                            → 400 Bad Request
   Yes ▼
[Signature is valid hex?]
   No  ──────────────────────────────► HmacValidationResult.Invalid("Malformed signature")
                                            → 400 Bad Request
   Yes ▼
[Compute server HMAC and compare (FixedTimeEquals)]
   Mismatch ────────────────────────► HmacValidationResult.Invalid("Signature mismatch")
                                            → 401 Unauthorized
   Match ──────────────────────────► HmacValidationResult.Valid(computedSig)
                                            → 200 OK
```

---

## Configuration Schema

```json
// appsettings.json
{
  "HMAC": {
    "SecretKey": "your-super-secret-key-change-me"
  }
}
```

Environment variable override (double-underscore notation, standard .NET):
```
HMAC__SecretKey=your-super-secret-key-change-me
```

---

## Relationships Summary

```
PaymentsController
  └── depends on → IHmacService (DIP)
                        └── implemented by → HmacService
                                                └── reads → IConfiguration (HMAC:SecretKey)
                                                └── returns → HmacValidationResult (value object)
                   uses → PaymentRequest (entity, deserialized from body)
```
