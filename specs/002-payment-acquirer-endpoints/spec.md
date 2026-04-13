# Feature Specification: Payment Acquirer Endpoints with HMAC SHA-256 Validation

**Feature Branch**: `002-payment-acquirer-endpoints`  
**Created**: 2026-04-12  
**Status**: Draft  
**Input**: User description: "o projeto tem que ser pensado em que tenha por exemplo, alguns endpoints, todos post como /authorizer /refound /void /confirmer e todos esses endpoints tenham a validação da requisição com o HMAC sha 256"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Terminal Authorizes a Payment (Priority: P1)

A developer (the learner) observes the payment terminal simulator sending a `POST /api/payments/authorize`
request with a properly signed HMAC SHA-256 signature. The backend validates the request integrity
before processing the authorization and responds with an approval. This is the most
common and critical transaction in a payment acquirer flow.

**Why this priority**: Authorization is the entry point of every payment transaction; it also
demonstrates the full HMAC request validation lifecycle that applies equally to all four endpoints.

**Independent Test**: Can be tested by sending a signed POST to `/api/payments/authorize` with
a valid payload and verifying `200 OK` with `{"status":"authorized"}`.

**Acceptance Scenarios**:

1. **Given** a valid payment payload signed with the shared secret, **When** `POST /api/payments/authorize`
   is called with the correct `X-Hmac-Signature` header, **Then** the backend responds `200 OK`
   with `{"status": "authorized", "transactionId": "<id>", "message": "Authorization approved."}`.
2. **Given** a tampered body (post-signing modification), **When** `POST /api/payments/authorize`
   is called, **Then** the backend responds `401 Unauthorized` with
   `{"status": "invalid", "message": "Signature mismatch. Request integrity compromised."}`.
3. **Given** no `X-Hmac-Signature` header, **When** `POST /api/payments/authorize` is called,
   **Then** the backend responds `400 Bad Request` with
   `{"status": "error", "message": "X-Hmac-Signature header is missing."}`.

---

### User Story 2 - Terminal Confirms an Authorized Transaction (Priority: P2)

After an authorization, the terminal sends a `POST /api/payments/confirm` to settle the
transaction. The backend validates the HMAC signature before confirming, demonstrating that
**every step of the payment flow** requires message integrity validation.

**Why this priority**: Confirmation closes the payment lifecycle; showing HMAC on every endpoint
reinforces that integrity validation is not optional at any stage.

**Independent Test**: Can be tested independently by sending a signed POST to
`/api/payments/confirm` with a valid `AuthorizationCode` and verifying the response.

**Acceptance Scenarios**:

1. **Given** a valid confirmation payload signed with the shared secret, **When**
   `POST /api/payments/confirm` is called, **Then** the backend responds `200 OK` with
   `{"status": "confirmed", "transactionId": "<id>", "message": "Transaction confirmed."}`.
2. **Given** a tampered confirmation request, **When** the endpoint is called, **Then**
   the backend responds `401 Unauthorized`.

---

### User Story 3 - Terminal Requests a Refund (Priority: P3)

The terminal sends a `POST /api/payments/refund` to reverse a previously confirmed transaction.
The HMAC validation prevents unauthorized or tampered refund requests.

**Why this priority**: Refund is a financially sensitive operation; demonstrating HMAC protection
on it explains why integrity validation matters beyond just authorization.

**Independent Test**: Can be tested independently by sending a signed POST to
`/api/payments/refund` with amount and original transaction reference.

**Acceptance Scenarios**:

1. **Given** a valid signed refund payload, **When** `POST /api/payments/refund` is called,
   **Then** the backend responds `200 OK` with
   `{"status": "refunded", "transactionId": "<id>", "message": "Refund processed."}`.
2. **Given** a request with a missing or invalid signature, **When** the endpoint is called,
   **Then** the backend responds with the appropriate `400` or `401` error.

---

### User Story 4 - Terminal Voids an Authorized (Unsettled) Transaction (Priority: P4)

The terminal sends a `POST /api/payments/void` to cancel an authorization before it is confirmed.
This demonstrates that the HMAC contract is consistent across all four payment lifecycle stages.

**Why this priority**: Void completes the full set of payment lifecycle endpoints; its inclusion
makes the lab a complete didactic picture of an acquirer flow.

**Independent Test**: Can be tested independently by sending a signed POST to
`/api/payments/void` with a valid authorization reference.

**Acceptance Scenarios**:

1. **Given** a valid signed void payload, **When** `POST /api/payments/void` is called,
   **Then** the backend responds `200 OK` with
   `{"status": "voided", "transactionId": "<id>", "message": "Authorization voided."}`.
2. **Given** a tampered void request, **When** the endpoint is called, **Then** the backend
   responds `401 Unauthorized`.

---

### Edge Cases

- What happens when all four endpoints receive a request with the same wrong signature? (Same HMAC filter is applied consistently — all four reject with `401`.)
- What if the body is empty `{}`? (HMAC computes over the empty JSON string bytes; field validation rejects with `400` before business logic.)
- What if the same body is sent to a different endpoint? (HMAC covers body only, not URL — documented as a learning note about including the HTTP method/path in production HMAC inputs.)
- What if the request is replayed? (Out of scope — replay/nonce protection is explicitly excluded for this didactic lab.)
- What if the configured `HMAC:SecretKey` is shorter than 32 characters? (Application MUST reject startup with a clear error message naming the minimum key-length; this is an intentional teaching moment about cryptographic key-strength requirements.)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST expose four HTTP POST endpoints, all requiring HMAC SHA-256 validation:
  - `POST /api/payments/authorize`
  - `POST /api/payments/confirm`
  - `POST /api/payments/refund`
  - `POST /api/payments/void`
- **FR-002**: All four endpoints MUST validate the `X-Hmac-Signature` request header against
  a server-computed HMAC SHA-256 of the raw request body before any business logic executes.
- **FR-003**: HMAC validation MUST be implemented as a single reusable action/endpoint filter
  (e.g., a `[ValidateHmac]` attribute or `AddEndpointFilter`) applied explicitly to each of the
  four endpoints — not duplicated per endpoint and not hidden in global middleware. This keeps
  the validation step visible and discoverable at the point of use (DRY / SRP / Didactic Clarity).
- **FR-004**: Each endpoint MUST accept a typed JSON payload specific to its business purpose:
  - **Authorize**: `TransactionId`, `Amount` (decimal), `MerchantId`, `CardToken` (non-empty string, no format constraint), `Timestamp`
  - **Confirm**: `TransactionId`, `AuthorizationCode`, `Timestamp`
  - **Refund**: `TransactionId`, `RefundAmount` (decimal), `Reason`, `Timestamp`
  - **Void**: `TransactionId`, `AuthorizationCode`, `Timestamp`
- **FR-005**: The HMAC shared secret key MUST be loaded from configuration (`HMAC:SecretKey` /
  env var `HMAC__SecretKey`); never hardcoded. The key MUST be at least 32 characters long;
  if the configured key is shorter, the application MUST fail at startup with a clear,
  educational error message explaining the minimum key-length requirement.
- **FR-006**: Signature comparison MUST use a timing-safe byte comparison to prevent side-channel attacks.
- **FR-007**: The backend MUST log each HMAC validation step at debug level (key loaded,
  body bytes, computed hash, comparison result) for all four endpoints.
- **FR-008**: A terminal simulator MUST demonstrate all four endpoints, sending both a valid
  and a tampered request for each (8 total HTTP calls).
- **FR-009**: The HMAC validation class/service and all four endpoint handlers MUST include
  documentation comments explaining the algorithm and each payment operation's purpose.
- **FR-010**: Each endpoint MUST return a JSON response containing `status`, `transactionId`,
  and `message` fields.

### Key Entities

- **AuthorizeRequest**: Payload for authorization; fields: `TransactionId` (Guid), `Amount` (decimal > 0), `MerchantId` (string), `CardToken` (non-empty string, no format enforcement — represents an opaque card token; format validation is out of scope for this HMAC lab), `Timestamp` (DateTimeOffset).
- **ConfirmRequest**: Payload for confirmation; fields: `TransactionId` (Guid), `AuthorizationCode` (string), `Timestamp` (DateTimeOffset).
- **RefundRequest**: Payload for refund; fields: `TransactionId` (Guid), `RefundAmount` (decimal > 0), `Reason` (string), `Timestamp` (DateTimeOffset).
- **VoidRequest**: Payload for void; fields: `TransactionId` (Guid), `AuthorizationCode` (string), `Timestamp` (DateTimeOffset).
- **HmacValidationResult**: Immutable value object shared by all endpoints; `IsValid` (bool), `Message` (string), `ReceivedSignature?` (string), `ComputedSignature?` (string).
- **PaymentResponse**: Typed response returned by all four endpoints; `Status` (string), `TransactionId` (Guid), `Message` (string).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can clearly see from the source code that HMAC validation is applied
  once and reused across all four endpoints, with no duplication.
- **SC-002**: All four endpoints correctly accept valid signed requests and reject tampered or
  unsigned requests in 100% of automated test cases.
- **SC-003**: All four user stories (Authorize, Confirm, Refund, Void) are covered by automated
  tests — valid and tampered scenarios — serving as executable documentation.
- **SC-004**: The terminal simulator demonstrates all four flows (8 requests) with zero manual
  configuration beyond setting `HMAC__SecretKey`.
- **SC-005**: A developer reading the code can identify the single place where HMAC validation
  lives and trace how it is wired to all four endpoints by reading no more than 3 files.

## Clarifications

### Session 2026-04-12

- Q: Should the spec require startup-time protection for weak HMAC keys? → A: Require minimum 32-character key — application fails at startup with a clear error message if key is shorter.
- Q: Which ASP.NET Core mechanism should implement HMAC validation (FR-003)? → A: Action/endpoint filter applied explicitly per endpoint (e.g., `[ValidateHmac]` or `AddEndpointFilter`) — keeps validation visible and discoverable at the point of use.
- Q: What format constraint applies to `CardToken` in `AuthorizeRequest`? → A: Non-empty string, no format enforcement — card-token format is out of scope for this HMAC-focused lab.

## Assumptions

- .NET 10 SDK is available in the development environment.
- All four endpoints are stateless for the lab — no persistence; responses are simulated
  (no real payment processing, no database).
- A single shared secret key applies to all four endpoints (no per-endpoint key differentiation).
- The HMAC is computed over the raw JSON request body only; HTTP method and URL path are not
  included in the signed message. This limitation is explicitly documented as a learning note in code.
- Clean Code, DDD-lite (rich domain objects), and SOLID are applied where they aid clarity.
  The four request types are distinct value objects to make each operation's intent explicit.
- "Refound" in the user's description is interpreted as "Refund" (standard payment industry terminology).
- Replay protection (timestamp/nonce validation) is explicitly out of scope for this didactic lab.
