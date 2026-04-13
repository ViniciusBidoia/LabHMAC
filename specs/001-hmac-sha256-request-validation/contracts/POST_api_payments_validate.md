# Contract: POST /api/payments/validate

**Feature**: `001-hmac-sha256-request-validation`  
**Date**: 2026-04-12  
**Endpoint**: `POST /api/payments/validate`

---

## Overview

This is the single public endpoint of the LabHMAC backend. It receives a JSON payment payload from
a payment terminal simulator, validates the HMAC SHA-256 message integrity signature carried in the
`X-Hmac-Signature` header, and returns the validation outcome.

---

## Request

### Headers

| Header | Required | Format | Description |
|--------|----------|--------|-------------|
| `Content-Type` | Yes | `application/json` | Body must be JSON |
| `X-Hmac-Signature` | Yes | lowercase hex (64 chars) | HMAC SHA-256 of the raw request body, computed with the shared secret key |

### Body

```json
{
  "transactionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "amount": 49.90,
  "merchantId": "MERCHANT-001",
  "timestamp": "2026-04-12T10:00:00+00:00"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `transactionId` | UUID (string) | Yes | Unique transaction identifier |
| `amount` | number (decimal) | Yes | Transaction amount; must be > 0 |
| `merchantId` | string | Yes | Merchant identifier |
| `timestamp` | ISO-8601 string | Yes | Terminal's request timestamp |

### How the terminal computes `X-Hmac-Signature`

```
1. Serialize the payment payload to a UTF-8 JSON string (the exact raw body bytes)
2. Encode the shared secret key as UTF-8 bytes
3. Compute HMAC-SHA256(keyBytes, bodyBytes) using System.Security.Cryptography.HMACSHA256
4. Convert the 32-byte result to a lowercase hex string (64 characters)
5. Set the header: X-Hmac-Signature: <hex-string>
```

**Critical**: The body bytes used for signing MUST be identical to the bytes sent in the HTTP
request body. Any difference in whitespace, key order, or encoding will produce a signature
mismatch on the server.

---

## Responses

### 200 OK — Valid request

```json
{
  "status": "valid",
  "message": "Request integrity verified."
}
```

**When**: `X-Hmac-Signature` header is present, well-formed, and matches the server-computed HMAC.

---

### 400 Bad Request — Missing header

```json
{
  "status": "error",
  "message": "X-Hmac-Signature header is missing."
}
```

**When**: The `X-Hmac-Signature` header is absent from the request.

---

### 400 Bad Request — Malformed signature

```json
{
  "status": "error",
  "message": "X-Hmac-Signature header value is not a valid hex string. Expected 64 lowercase hex characters."
}
```

**When**: The header is present but its value is not a valid lowercase 64-character hex string.

---

### 401 Unauthorized — Signature mismatch

```json
{
  "status": "invalid",
  "message": "Signature mismatch. Request integrity compromised."
}
```

**When**: The received signature does not match the server-computed HMAC (body was tampered,
wrong key, or encoding mismatch).

---

## Security Notes

- **Timing-safe comparison**: The server uses `CryptographicOperations.FixedTimeEquals` to compare
  signature bytes. This prevents timing-based side-channel attacks where an attacker could infer
  correct signature bytes one at a time by measuring response time.
- **Secret key**: The shared secret is never returned in any response. It is loaded from server
  configuration only (`HMAC:SecretKey` / `HMAC__SecretKey` env var).
- **No replay protection**: This is a didactic lab — nonce/timestamp replay protection is
  deliberately out of scope. A production system would also validate the `timestamp` field.

---

## Simulator Usage Example

The `LabHMAC.Simulator` console app demonstrates both scenarios:

```
dotnet run --project src/LabHMAC.Simulator -- --url https://localhost:5001
```

Output (valid request):
```
[SIMULATOR] Sending valid request...
[SIMULATOR] Body: {"transactionId":"...","amount":49.90,...}
[SIMULATOR] X-Hmac-Signature: a3f7c1...
[SIMULATOR] Response: 200 OK — {"status":"valid","message":"Request integrity verified."}

[SIMULATOR] Sending tampered request...
[SIMULATOR] Body (tampered): {"transactionId":"...","amount":999.99,...}
[SIMULATOR] X-Hmac-Signature: a3f7c1... (original, unmodified)
[SIMULATOR] Response: 401 Unauthorized — {"status":"invalid","message":"Signature mismatch..."}
```
