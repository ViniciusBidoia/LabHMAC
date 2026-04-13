# Implementation Plan: HMAC SHA-256 Request Validation Lab

**Branch**: `001-hmac-sha256-request-validation` | **Date**: 2026-04-12 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/001-hmac-sha256-request-validation/spec.md`

## Summary

Build a didactic ASP.NET Core 10 Web API that validates incoming HTTP POST requests using HMAC
SHA-256 message integrity. A companion terminal simulator (console app) demonstrates both valid
and tampered request flows. Every cryptographic step is deliberately isolated, named, and commented
to serve as a learning resource. The project applies Clean Code, DDD-lite (rich value objects,
domain service), and SOLID (SRP, OCP, DIP via interface) only where they reinforce clarity.

## Technical Context

**Language/Version**: C# 13 / .NET 10  
**Primary Dependencies**: ASP.NET Core 10 Web API (BCL only — `System.Security.Cryptography.HMACSHA256`, `CryptographicOperations.FixedTimeEquals`)  
**Storage**: N/A — stateless request validation  
**Testing**: xUnit 2.x — unit tests for domain, integration tests for HTTP pipeline  
**Target Platform**: Local development (Windows / Linux / macOS)  
**Project Type**: web-service (ASP.NET Core Web API) + console simulator  
**Performance Goals**: Negligible — sub-10 ms per validation; not a production performance concern  
**Constraints**: BCL only for crypto; two .NET projects max (API + Simulator); no third-party auth/crypto libs  
**Scale/Scope**: Single developer learning scenario — 1 endpoint, ~5 source files, ~10 test cases

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence |
|-----------|--------|----------|
| I. Didactic Clarity | ✅ PASS | Every HMAC step split into named methods with XML doc + inline comments explaining *why* |
| II. HMAC-First Design | ✅ PASS | `IHmacService` / `HmacService` isolated; never inlined in controller/middleware |
| III. Test-First | ✅ PASS | xUnit tests planned before implementation; test names describe scenarios in plain language |
| IV. Simplicity Over Architecture | ✅ PASS | Single solution, two projects (API + Simulator), BCL only, no CQRS/MediatR/Repository |

**DDD & SOLID alignment note**: DDD is applied *didactically* — `PaymentRequest` and
`HmacValidationResult` are rich domain objects with behaviour, not anemic DTOs. SOLID means:
one class = one responsibility (SRP), and `IHmacService` enables easy unit-testing (DIP).
No over-engineering beyond what aids learning.

**Post-Phase-1 re-check**: ✅ No new violations introduced by data model or contracts.

## Project Structure

### Documentation (this feature)

```text
specs/001-hmac-sha256-request-validation/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── POST_api_payments_validate.md   # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
└── LabHMAC.Api/
    ├── LabHMAC.Api.csproj
    ├── Program.cs
    ├── Domain/
    │   ├── PaymentRequest.cs          # Rich domain entity (FR-001, FR-008)
    │   ├── HmacValidationResult.cs    # Value object (FR-002, FR-008)
    │   └── IHmacService.cs            # Domain service interface (DIP / FR-003)
    ├── Application/
    │   └── HmacService.cs             # Domain service implementation (FR-003–FR-006)
    ├── Api/
    │   └── PaymentsController.cs      # Single endpoint POST /api/payments/validate (FR-001)
    └── appsettings.json               # HMAC__SecretKey config entry (FR-004)

src/
└── LabHMAC.Simulator/
    ├── LabHMAC.Simulator.csproj
    └── Program.cs                     # Sends valid + tampered requests (FR-007)

tests/
└── LabHMAC.Tests/
    ├── LabHMAC.Tests.csproj
    ├── Unit/
    │   ├── HmacServiceTests.cs        # Unit tests for signing & verification
    │   └── HmacValidationResultTests.cs
    └── Integration/
        └── PaymentsEndpointTests.cs   # HTTP pipeline tests (all 3 user stories)
```

**Structure Decision**: Single solution (`LabHMAC.sln`) with three projects. Two *src* projects
(API + Simulator) keep the learning scenario self-contained. One *tests* project covers both unit
and integration concerns. No `frontend/` — purely backend/console. This is the minimum structure
that makes the educational scenario runnable end-to-end.
