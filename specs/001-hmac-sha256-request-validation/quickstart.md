# Quickstart: LabHMAC

**Feature**: `001-hmac-sha256-request-validation`  
**Date**: 2026-04-12

A didactic ASP.NET Core 10 lab demonstrating HMAC SHA-256 request integrity validation,
simulating a payment terminal calling a backend.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git

---

## 1. Clone and restore

```bash
git clone <repo-url> LabHMAC
cd LabHMAC
dotnet restore
```

---

## 2. Configure the shared secret key

Edit `src/LabHMAC.Api/appsettings.json`:

```json
{
  "HMAC": {
    "SecretKey": "minha-chave-secreta-super-segura"
  }
}
```

Or use an environment variable:

```bash
# Linux / macOS
export HMAC__SecretKey="minha-chave-secreta-super-segura"

# Windows (PowerShell)
$env:HMAC__SecretKey = "minha-chave-secreta-super-segura"
```

> **Why double underscore?** .NET configuration maps `__` to `:` in section separators.
> `HMAC__SecretKey` → `HMAC:SecretKey`.

---

## 3. Run the backend

```bash
dotnet run --project src/LabHMAC.Api
# Listening on https://localhost:5001
```

---

## 4. Run the simulator

Open a second terminal in the same project root:

```bash
dotnet run --project src/LabHMAC.Simulator
```

The simulator will:
1. Send a **valid signed request** → expect `200 OK`
2. Send a **tampered request** (body modified after signing) → expect `401 Unauthorized`
3. Send a **request without the signature header** → expect `400 Bad Request`

The simulator output shows every step: body, computed signature, HTTP response.

---

## 5. Run the tests

```bash
dotnet test
```

Expected output:
```
Passed!  LabHMAC.Tests → Unit: HmacService_ComputesCorrectSignature
Passed!  LabHMAC.Tests → Unit: HmacService_ValidRequest_ReturnsValid
Passed!  LabHMAC.Tests → Unit: HmacService_TamperedBody_ReturnsInvalid
Passed!  LabHMAC.Tests → Integration: Endpoint_ValidRequest_Returns200
Passed!  LabHMAC.Tests → Integration: Endpoint_TamperedRequest_Returns401
Passed!  LabHMAC.Tests → Integration: Endpoint_MissingHeader_Returns400
```

---

## 6. Understanding the code (learning path)

Read the files in this order to follow the HMAC flow from algorithm to HTTP:

1. `src/LabHMAC.Api/Domain/IHmacService.cs`  
   The contract: what the service does, in plain C# comments.

2. `src/LabHMAC.Api/Application/HmacService.cs`  
   The implementation: each HMAC step (key → bytes → HMACSHA256 → hex → FixedTimeEquals)
   is a separate method with comments explaining why it's done that way.

3. `src/LabHMAC.Api/Domain/HmacValidationResult.cs`  
   The value object: immutable result with named factory methods.

4. `src/LabHMAC.Api/Domain/PaymentRequest.cs`  
   The domain entity: the payload being protected.

5. `src/LabHMAC.Api/Api/PaymentsController.cs`  
   The endpoint: thin controller that delegates to `IHmacService`.

6. `src/LabHMAC.Simulator/Program.cs`  
   The client: shows exactly how a terminal computes and sends the signature.

7. `tests/LabHMAC.Tests/Unit/HmacServiceTests.cs`  
   The tests: each test is a scenario description in plain English.

---

## Key Concept: Why HMAC?

HMAC (Hash-based Message Authentication Code) answers the question:  
**"Did this message arrive unmodified, from someone who knows the shared secret?"**

```
Terminal                                Backend
   │                                       │
   │  1. Compute HMAC(secret, body)        │
   │  2. POST /api/payments/validate       │
   │     Body: {payment payload}  ─────────►│  3. Read X-Hmac-Signature header
   │     X-Hmac-Signature: <hex> ─────────►│  4. Compute HMAC(secret, body) server-side
   │                                       │  5. Compare (timing-safe)
   │◄─── 200 OK / 401 Unauthorized ────────│  6. Accept or reject
```

If anyone modifies the body in transit (step 2 → 3), the server's computed HMAC (step 4)
will not match the signature computed by the terminal (step 1) → **401 Unauthorized**.

---

## Project Structure (quick reference)

```
src/
├── LabHMAC.Api/          ← Backend (ASP.NET Core 10)
│   ├── Domain/           ← PaymentRequest, HmacValidationResult, IHmacService
│   ├── Application/      ← HmacService (crypto implementation)
│   └── Api/              ← PaymentsController (HTTP layer)
└── LabHMAC.Simulator/    ← Terminal simulator (console app)

tests/
└── LabHMAC.Tests/
    ├── Unit/             ← Tests for HmacService in isolation
    └── Integration/      ← Full HTTP pipeline tests (WebApplicationFactory)

specs/
└── 001-hmac-sha256-request-validation/
    ├── spec.md           ← Feature requirements
    ├── plan.md           ← This implementation plan
    ├── research.md       ← Technical decisions & rationale
    ├── data-model.md     ← Domain objects & relationships
    ├── contracts/        ← API endpoint contract
    └── quickstart.md     ← This file
```
