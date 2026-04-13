# Feature Specification: HMAC SHA-256 Request Validation Lab

**Feature Branch**: `001-hmac-sha256-request-validation`  
**Created**: 2026-04-12  
**Status**: Draft  
**Input**: User description: "Projeto extremamente simples e didatico, para entendimento de desenvolvedores de como funciona o HMAC SHA 256. Projeto para simular uma aplicação que valida o request, com o HMAC para saber se o request é integro. Como se fosse uma maquininha de adquirencia chamando um Backend."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Terminal Sends a Valid Signed Request (Priority: P1)

A developer (the learner) runs the project and sees a payment terminal simulator sending an HTTP
POST request to the backend. The request body contains a payment payload and the HTTP header
`X-Hmac-Signature` carries an HMAC SHA-256 signature computed from the body using a shared secret
key. The backend verifies the signature and returns `200 OK` with a confirmation message.

**Why this priority**: This is the core learning scenario — seeing the full happy-path flow of HMAC
request signing and server-side validation is the primary educational goal.

**Independent Test**: Can be fully tested by running backend + terminal simulator and observing the
`200 OK` response and log output showing each HMAC step.

**Acceptance Scenarios**:

1. **Given** a shared secret key is configured on both terminal and backend, **When** the terminal
   POSTs a payment payload with a correctly computed `X-Hmac-Signature` header, **Then** the
   backend responds `200 OK` with `{"status": "valid", "message": "Request integrity verified."}`.
2. **Given** the request is valid, **When** the backend processes it, **Then** structured log lines
   show each step: key-loading, payload-encoding, HMAC computation, signature comparison result.

---

### User Story 2 - Terminal Sends a Tampered Request (Priority: P2)

The developer intentionally tampers with the request body after signing (simulating a
man-in-the-middle attack) and observes the backend reject it.

**Why this priority**: Seeing the failure case is essential for understanding *why* HMAC provides
integrity protection — it complements the success path.

**Independent Test**: Can be tested by calling the backend endpoint with a body whose content
differs from what was signed.

**Acceptance Scenarios**:

1. **Given** a payload signed with the correct secret, **When** the body is modified before sending
   and the original signature header is kept, **Then** the backend responds `401 Unauthorized` with
   `{"status": "invalid", "message": "Signature mismatch. Request integrity compromised."}`.
2. **Given** the invalid request, **When** the backend processes it, **Then** logs show the
   computed server-side HMAC and the received signature side-by-side, making the mismatch visible.

---

### User Story 3 - Missing or Malformed Signature Header (Priority: P3)

The developer sends a request without the `X-Hmac-Signature` header (or with a malformed value)
and observes a clear, educational error response.

**Why this priority**: Guards against accidental misuse and teaches proper header contract usage.

**Independent Test**: Can be tested with a plain HTTP POST that has no `X-Hmac-Signature` header.

**Acceptance Scenarios**:

1. **Given** no `X-Hmac-Signature` header, **When** the backend receives the POST, **Then** it
   responds `400 Bad Request` with `{"status": "error", "message": "X-Hmac-Signature header is missing."}`.
2. **Given** a malformed (non-hex) signature value, **When** the backend receives the POST, **Then**
   it responds `400 Bad Request` with an educational message explaining the expected format.

---

### Edge Cases

- What happens when the payload is empty (`{}`)?
- What happens when the shared secret key is an empty string?
- How does timing-safe comparison prevent timing attacks? (documented in code comments)
- What if the `Content-Type` is not `application/json`?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST expose a single HTTP POST endpoint (e.g., `/api/payments/validate`)
  that accepts a JSON payment payload.
- **FR-002**: System MUST read the `X-Hmac-Signature` request header and compare it against a
  server-computed HMAC SHA-256 signature of the raw request body.
- **FR-003**: HMAC signing logic MUST use `System.Security.Cryptography.HMACSHA256` from the BCL.
- **FR-004**: The HMAC key MUST be loaded from configuration (`appsettings.json` /
  environment variable `HMAC__SecretKey`) — never hardcoded.
- **FR-005**: Signature comparison MUST use a timing-safe byte comparison (`CryptographicOperations.FixedTimeEquals`).
- **FR-006**: The backend MUST log each step of the HMAC verification process at `Debug` level
  (key loaded, body bytes, computed hash, comparison result).
- **FR-007**: A terminal simulator (console app or HTTP client script) MUST be included to
  demonstrate both valid and tampered request scenarios.
- **FR-008**: All HMAC-related classes MUST include XML doc comments explaining the algorithm step.

### Key Entities

- **PaymentRequest**: Simulated payment payload; fields: `TransactionId` (Guid), `Amount` (decimal),
  `MerchantId` (string), `Timestamp` (DateTimeOffset). Represents what a payment terminal would send.
- **HmacValidationResult**: Value object; fields: `IsValid` (bool), `Message` (string), optional
  `ReceivedSignature` and `ComputedSignature` for diagnostic logging.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer unfamiliar with HMAC can read the source code top-to-bottom and
  understand every step of the algorithm without external documentation.
- **SC-002**: The backend correctly accepts valid requests and rejects tampered ones in 100% of
  automated test cases.
- **SC-003**: All three user story scenarios (valid, tampered, missing header) are covered by
  automated tests that serve as executable documentation.
- **SC-004**: Running `dotnet run` and the terminal simulator requires zero configuration beyond
  setting the `HMAC__SecretKey` value.

## Assumptions

- .NET 10 SDK is available in the development environment.
- The project targets a didactic audience; production concerns like rate limiting, persistence, and
  OAuth are explicitly out of scope.
- A single shared secret key is sufficient for the demo (no per-client key management).
- The payment payload is always JSON; binary payloads are out of scope.
- Clean Code and DDD principles are applied where they aid clarity — not as architecture for its
  own sake. In a project this small, DDD means: rich domain objects with behaviour, not anemic
  models; SOLID means: one class = one reason to change.
