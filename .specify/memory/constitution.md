<!--
SYNC IMPACT REPORT
==================
Version change: (template) → 1.0.0 (initial ratification)

Modified principles:
  - All principles: newly created from template placeholders

Added sections:
  - Core Principles (4 principles)
  - Technology Stack & Constraints
  - Development Workflow
  - Governance

Removed sections:
  - N/A (first ratification)

Templates:
  - .specify/templates/plan-template.md     ✅ Reviewed — Constitution Check section will reference these principles; no structural change needed
  - .specify/templates/spec-template.md     ✅ Reviewed — Generic structure aligns with Didactic Clarity and HMAC-First principles
  - .specify/templates/tasks-template.md    ✅ Reviewed — Task organization aligns with Simplicity Over Architecture principle
  - README.md                               ✅ Reviewed — Minimal; consistent with project scope

Follow-up TODOs:
  - None — all placeholders resolved
-->

# LabHMAC Constitution

## Core Principles

### I. Didactic Clarity (NON-NEGOTIABLE)

Every line of code MUST serve as a learning aid for developers studying HMAC SHA-256.
Code MUST be explicit, readable, and step-by-step — never clever at the cost of comprehension.
Every HMAC computation step (key preparation, message encoding, signing, comparison) MUST be separated
into clearly named methods or classes with comments explaining **why**, not just **what**.
Premature optimization is forbidden; intent is paramount.

**Rationale**: The sole purpose of this project is education. Obscure or terse code defeats the goal
entirely, even if functionally correct.

### II. HMAC-First Design

All request integrity validation MUST use HMAC SHA-256 and nothing else.
The HMAC signing and verification logic MUST live in a dedicated, isolated service/class
(e.g., `HmacService` or `HmacValidator`) — it MUST NOT be embedded inline in controllers or middleware.
No alternative integrity or authentication mechanisms are permitted in this project.

**Rationale**: Isolation of the HMAC logic makes the algorithm easy to find, read, and study
in isolation, which is the whole point of the lab.

### III. Test-First (NON-NEGOTIABLE)

Tests MUST be written before implementation (TDD: Red → Green → Refactor).
Unit tests MUST cover HMAC generation and verification in isolation.
Integration tests MUST cover the full HTTP request validation flow (simulating the payment terminal
calling the backend).
Tests serve as executable documentation — test names MUST describe the scenario in plain language.

**Rationale**: Tests make the expected behaviour unambiguous and give developers a second artifact
(besides the code) to learn from.

### IV. Simplicity Over Architecture

YAGNI is strictly enforced. The project MUST remain a single .NET project (no multi-project solutions
unless absolutely required by the demo scenario).
No unnecessary layers of abstraction (no Repository pattern, no MediatR, no CQRS).
Dependencies MUST be kept to the minimum required by the demo (essentially the .NET BCL only).
If complexity is added, it MUST be justified with a written comment explaining why the simpler
alternative was insufficient.

**Rationale**: Architecture patterns obscure the HMAC mechanics for learners. Flat, direct code
keeps focus on the cryptographic concept being demonstrated.

## Technology Stack & Constraints

- **Platform**: .NET 8+ (ASP.NET Core Web API)
- **Language**: C# — idiomatic, using BCL `System.Security.Cryptography.HMACSHA256`
- **Testing framework**: xUnit (preferred) or NUnit
- **Dependencies**: BCL only; no third-party crypto libraries
- **Project structure**: Single solution, single project (`LabHMAC`)
- **Target audience**: .NET developers learning HMAC SHA-256 for the first time
- **Scenario**: Payment terminal (acquirer) → Backend validation, simulated via HTTP POST with
  HMAC signature in request header (e.g., `X-Hmac-Signature`)

## Development Workflow

- Each feature/change starts from a spec or a clearly described scenario.
- Implementation follows TDD: write failing tests first, then implement.
- Pull requests MUST include a brief description of which HMAC concept the change illustrates.
- Code review MUST verify: (a) Didactic Clarity compliance, (b) no added unnecessary complexity,
  (c) tests pass and are self-describing.
- No feature is merged unless its educational intent is clear from the code alone.

## Governance

This constitution supersedes all implicit conventions and personal preferences.
Amendments require: (1) a written rationale explaining the educational need, (2) a version bump
following semantic versioning rules, and (3) a propagation check against all dependent templates.

All implementation plans (`plan.md`) MUST include a Constitution Check gate validated against
these four principles before Phase 0 research begins.

Complexity MUST be justified inline. Any deviation from Simplicity Over Architecture MUST appear
in the plan's Complexity Tracking table.

**Version**: 1.0.0 | **Ratified**: 2026-04-12 | **Last Amended**: 2026-04-12
