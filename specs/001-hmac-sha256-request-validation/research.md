# Research: HMAC SHA-256 Request Validation Lab

**Feature**: `001-hmac-sha256-request-validation`  
**Date**: 2026-04-12  
**Status**: Complete — all NEEDS CLARIFICATION resolved

---

## 1. HMAC SHA-256 Algorithm Mechanics

### Decision
Use `System.Security.Cryptography.HMACSHA256` (BCL) directly — no wrapper library.

### Rationale
The BCL implementation is the canonical choice for .NET. Using it directly exposes learners to
the actual API they will encounter in production code. Third-party libraries (BouncyCastle, etc.)
add indirection that hides the mechanism.

**Algorithm steps for the didactic implementation:**

1. **Encode key**: Convert the shared secret string to `byte[]` using `Encoding.UTF8`.
2. **Encode message**: Convert the raw request body string to `byte[]` using `Encoding.UTF8`.
3. **Compute HMAC**: Call `HMACSHA256.HashData(keyBytes, messageBytes)` → produces 32-byte hash.
4. **Encode result**: Convert hash bytes to lowercase hex string (`Convert.ToHexString(hash).ToLower()`).
5. **Compare**: Use `CryptographicOperations.FixedTimeEquals` on both hash byte arrays to prevent
   timing attacks. Never use `string ==` or `string.Equals` for this comparison.

### Alternatives considered
- **JWT with HMAC-SHA256**: Rejected — JWTs add base64url encoding and a structured payload on top
  of HMAC; they obscure the raw HMAC mechanism, defeating the educational goal.
- **BouncyCastle**: Rejected — external dependency; BCL is sufficient and more instructive.

---

## 2. Shared Secret Key Management (for the lab)

### Decision
Load the HMAC secret key from `IConfiguration` via `appsettings.json` entry `HMAC:SecretKey`,
overridable by environment variable `HMAC__SecretKey` (double underscore = section separator
in .NET config system).

### Rationale
Even in a lab, hardcoding secrets is a bad habit to teach. Using `IConfiguration` introduces
developers to the correct .NET pattern at essentially zero cost and is consistent with
the didactic goal (learn the right way, not just any way).

### Alternatives considered
- Hardcoded constant: Rejected — teaches anti-pattern; violates FR-004.
- `IOptions<HmacOptions>` with `[Required]`: Good practice but adds a class for one field;
  acceptable to include as an optional enhancement, but direct `IConfiguration` keeps Phase 1 lean.

---

## 3. Reading Raw Request Body in ASP.NET Core

### Decision
Enable `Request.EnableBuffering()` in middleware (or use `[FromBody] string` with a custom
input formatter) so the raw body bytes can be read before model binding, allowing HMAC
computation on the unmodified payload.

**Chosen approach**: Use a custom `HmacValidationFilter` (IActionFilter or IEndpointFilter)
that reads `HttpContext.Request.Body` after calling `Request.EnableBuffering()`, resets the
stream position, and passes the raw body to `IHmacService.Validate(...)`.

### Rationale
Reading the body in a filter keeps the controller clean (Single Responsibility) and makes the
HMAC validation step explicit and visible — a learner can find it in one place.

### Alternatives considered
- **Custom middleware**: Also valid, but middleware runs for all routes; a filter scoped to the
  endpoint is more precise and more instructive for showing "this endpoint requires HMAC".
- **Reading `[FromBody]` + re-serializing**: Rejected — re-serializing JSON can alter whitespace/
  key order, producing a different byte sequence than what the terminal signed.

---

## 4. Timing-Safe Comparison

### Decision
Use `CryptographicOperations.FixedTimeEquals(ReadOnlySpan<byte>, ReadOnlySpan<byte>)` from
`System.Security.Cryptography` (available since .NET 5).

### Rationale
String equality short-circuits on the first mismatched character, leaking timing information
that can be exploited to reconstruct the expected signature byte-by-byte. `FixedTimeEquals`
runs in constant time regardless of where the mismatch occurs. Demonstrating this in a lab is
an important security education moment.

### Alternatives considered
- `string.Equals(a, b, StringComparison.Ordinal)`: Rejected — not timing-safe.
- Custom byte loop with artificial delay: Rejected — fragile and imprecise; BCL provides the
  right tool.

---

## 5. .NET 10 + Clean Code + DDD-lite + SOLID

### Decision
Apply these principles *minimally and didactically*:

| Pattern | Application | Scope |
|---------|------------|-------|
| Clean Code | Meaningful names, small methods, no magic numbers/strings | All files |
| DDD-lite | `PaymentRequest` entity with `Validate()` logic; `HmacValidationResult` as immutable value object | `Domain/` |
| SRP | `HmacService` does only HMAC; `PaymentsController` does only HTTP routing | All classes |
| OCP | `IHmacService` interface allows swapping algorithm without touching controller | Interface + impl |
| DIP | Controller depends on `IHmacService`, not `HmacService` concrete type | DI container |
| LSP/ISP | Not forced — one interface, one method is naturally compliant | `IHmacService` |

### Rationale
The user explicitly requested Clean Code + DDD + SOLID. Applied conservatively so patterns
illuminate rather than obscure. Overuse of DDD patterns (Aggregates, Events, Repositories) in
a two-entity project would violate Constitution Principle IV (Simplicity).

### Alternatives considered
- Full DDD with Aggregates/Domain Events: Rejected — massive overhead for 2 entities; obscures HMAC logic.
- No patterns at all (flat procedural code): Rejected — user explicitly requested these patterns.

---

## 6. Integration Test Strategy (ASP.NET Core `WebApplicationFactory`)

### Decision
Use `Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>` for integration tests.
This spins up the full HTTP pipeline in-memory, allowing tests to send real HTTP requests with
crafted headers and bodies without running an actual server.

### Rationale
This is the canonical ASP.NET Core integration testing approach. It gives learners a complete,
runnable example of how to test API endpoints, and ensures the HMAC validation filter is tested
in its actual execution context (not mocked away).

### Alternatives considered
- Pure unit tests of the filter: Rejected for integration tests — testing the filter in isolation
  doesn't verify that it's correctly wired into the pipeline.
- Starting a real HTTP server with `TestServer`: `WebApplicationFactory` is the modern abstraction
  over `TestServer`; preferred.

---

## Resolved Unknowns Summary

| Was | Resolved To |
|-----|-------------|
| How to read raw body | `EnableBuffering()` + `IActionFilter` / `IEndpointFilter` |
| How to compare signatures | `CryptographicOperations.FixedTimeEquals` |
| Where to put secret key | `appsettings.json:HMAC:SecretKey` / env var `HMAC__SecretKey` |
| Integration test mechanism | `WebApplicationFactory<Program>` (xUnit) |
| DDD scope | Domain-lite: 2 domain objects + 1 domain service interface |
