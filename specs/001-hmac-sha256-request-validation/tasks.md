# Tasks: HMAC SHA-256 Request Validation Lab

**Input**: Design documents from `/specs/001-hmac-sha256-request-validation/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: Included — explicitly required by spec (SC-002, SC-003) and constitution (Principle III: Test-First).

**Organization**: Tasks grouped by user story. Each story is independently testable after its phase completes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths included in all descriptions

---

## Phase 1: Setup (Project Initialization)

**Purpose**: Create the .NET 10 solution structure with three projects per plan.md

- [ ] T001 Create LabHMAC.sln solution file and directory structure (src/, tests/) at repository root
- [ ] T002 Create LabHMAC.Api web API project (net10.0) in src/LabHMAC.Api/LabHMAC.Api.csproj with Domain/, Application/, and Api/ folders
- [ ] T003 [P] Create LabHMAC.Simulator console project (net10.0) in src/LabHMAC.Simulator/LabHMAC.Simulator.csproj
- [ ] T004 [P] Create LabHMAC.Tests xUnit project (net10.0) in tests/LabHMAC.Tests/LabHMAC.Tests.csproj with project reference to LabHMAC.Api and Microsoft.AspNetCore.Mvc.Testing package

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain objects, HMAC service, and application configuration — MUST complete before any user story

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T005 [P] Create PaymentRequest entity with TransactionId, Amount, MerchantId, Timestamp fields and IsValid() method in src/LabHMAC.Api/Domain/PaymentRequest.cs
- [ ] T006 [P] Create HmacValidationResult value object with IsValid, Message, ReceivedSignature, ComputedSignature properties and static factory methods (Valid, Invalid, MissingHeader) in src/LabHMAC.Api/Domain/HmacValidationResult.cs
- [ ] T007 [P] Create IHmacService interface with ComputeSignature(string) and Validate(string, string?) methods in src/LabHMAC.Api/Domain/IHmacService.cs
- [ ] T008 Implement HmacService using HMACSHA256.HashData, Convert.ToHexString, and CryptographicOperations.FixedTimeEquals with Debug logging per FR-006 in src/LabHMAC.Api/Application/HmacService.cs
- [ ] T009 [P] Configure HMAC:SecretKey section with placeholder value in src/LabHMAC.Api/appsettings.json
- [ ] T010 Configure Program.cs with HmacService DI registration, request buffering middleware (EnableBuffering), and JSON serialization in src/LabHMAC.Api/Program.cs

**Checkpoint**: Foundation ready — domain objects compiled, HmacService registered, API pipeline configured

---

## Phase 3: User Story 1 — Terminal Sends a Valid Signed Request (Priority: P1) 🎯 MVP

**Goal**: Build the POST /api/payments/validate endpoint with HMAC validation filter and verify the happy-path flow returns 200 OK

**Independent Test**: Run backend → send a request with correct X-Hmac-Signature → observe 200 OK with `{"status":"valid","message":"Request integrity verified."}`

### Tests for User Story 1

> **Write these tests FIRST, ensure they FAIL before implementation of T013–T014**

- [ ] T011 [P] [US1] Write unit tests for HmacService: ComputeSignature returns correct hex, Validate with matching signature returns Valid result in tests/LabHMAC.Tests/Unit/HmacServiceTests.cs
- [ ] T012 [P] [US1] Write unit tests for HmacValidationResult: Valid() sets IsValid=true, Invalid() sets IsValid=false, MissingHeader() returns correct message in tests/LabHMAC.Tests/Unit/HmacValidationResultTests.cs

### Implementation for User Story 1

- [ ] T013 [US1] Create HmacValidationFilter (IEndpointFilter or IActionFilter) that reads raw body via EnableBuffering, extracts X-Hmac-Signature header, and delegates to IHmacService.Validate in src/LabHMAC.Api/Api/HmacValidationFilter.cs
- [ ] T014 [US1] Create PaymentsController with POST /api/payments/validate endpoint, apply HmacValidationFilter, deserialize PaymentRequest, and return appropriate status codes (200/400/401) in src/LabHMAC.Api/Api/PaymentsController.cs
- [ ] T015 [US1] Write integration test using WebApplicationFactory for valid signed request → 200 OK with expected JSON body in tests/LabHMAC.Tests/Integration/PaymentsEndpointTests.cs

**Checkpoint**: User Story 1 fully functional — valid HMAC requests return 200 OK, all US1 tests pass

---

## Phase 4: User Story 2 — Terminal Sends a Tampered Request (Priority: P2)

**Goal**: Verify that a request with body modified after signing returns 401 Unauthorized with signature mismatch message

**Independent Test**: Send a request where body differs from what was signed → observe 401 Unauthorized with `{"status":"invalid","message":"Signature mismatch. Request integrity compromised."}`

### Tests for User Story 2

- [ ] T016 [US2] Add unit test for HmacService.Validate with tampered body (signature computed from original body, validated against modified body) → returns Invalid result in tests/LabHMAC.Tests/Unit/HmacServiceTests.cs
- [ ] T017 [US2] Add integration test using WebApplicationFactory for tampered request → 401 Unauthorized with expected JSON body in tests/LabHMAC.Tests/Integration/PaymentsEndpointTests.cs

**Checkpoint**: User Stories 1 AND 2 verified — valid → 200, tampered → 401

---

## Phase 5: User Story 3 — Missing or Malformed Signature Header (Priority: P3)

**Goal**: Verify that requests without X-Hmac-Signature or with malformed (non-hex) values return 400 Bad Request with educational error messages

**Independent Test**: Send request without header → 400; send request with non-hex signature → 400

### Tests for User Story 3

- [ ] T018 [P] [US3] Add unit test for HmacService.Validate with null/empty signature → returns MissingHeader result in tests/LabHMAC.Tests/Unit/HmacServiceTests.cs
- [ ] T019 [P] [US3] Add unit test for HmacValidationFilter or HmacService handling malformed hex signature → returns Invalid result with format message in tests/LabHMAC.Tests/Unit/HmacServiceTests.cs
- [ ] T020 [P] [US3] Add integration test for missing X-Hmac-Signature header → 400 Bad Request with `"X-Hmac-Signature header is missing."` in tests/LabHMAC.Tests/Integration/PaymentsEndpointTests.cs
- [ ] T021 [P] [US3] Add integration test for malformed (non-hex) signature → 400 Bad Request with educational format message in tests/LabHMAC.Tests/Integration/PaymentsEndpointTests.cs

**Checkpoint**: All three user stories verified — valid → 200, tampered → 401, missing/malformed → 400

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Terminal simulator and end-to-end validation

- [ ] T022 Implement terminal simulator demonstrating all three scenarios (valid, tampered, missing header) with step-by-step console output showing HMAC computation in src/LabHMAC.Simulator/Program.cs
- [ ] T023 Run quickstart.md end-to-end validation: dotnet build, dotnet test (all pass), run API + simulator, verify all three scenarios produce expected output

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Phase 2 — delivers MVP
- **US2 (Phase 4)**: Depends on Phase 3 (endpoint must exist for integration tests)
- **US3 (Phase 5)**: Depends on Phase 3 (endpoint must exist for integration tests)
- **Polish (Phase 6)**: Depends on all user story phases

### User Story Dependencies

- **US1 (P1)**: Requires Foundational phase — builds the endpoint and proves the happy path
- **US2 (P2)**: Requires US1 endpoint — adds tampered-request test coverage (no new implementation code)
- **US3 (P3)**: Requires US1 endpoint — adds missing/malformed header test coverage (no new implementation code)

### Within Each User Story

- Unit tests written FIRST (should fail or test foundational code)
- Implementation after tests
- Integration tests after implementation
- Story checkpoint before next priority

### Parallel Opportunities

**Phase 1**: T003 ‖ T004 (simulator and test projects are independent)
**Phase 2**: T005 ‖ T006 ‖ T007 ‖ T009 (domain objects and config — separate files)
**Phase 3**: T011 ‖ T012 (unit tests — separate files)
**Phase 5**: T018 ‖ T019 ‖ T020 ‖ T021 (all test additions — independent scenarios)

---

## Parallel Example: Phase 2 (Foundational)

```text
# Launch all domain objects in parallel:
Task T005: "Create PaymentRequest entity in src/LabHMAC.Api/Domain/PaymentRequest.cs"
Task T006: "Create HmacValidationResult value object in src/LabHMAC.Api/Domain/HmacValidationResult.cs"
Task T007: "Create IHmacService interface in src/LabHMAC.Api/Domain/IHmacService.cs"
Task T009: "Configure HMAC:SecretKey in src/LabHMAC.Api/appsettings.json"

# Then sequential (depends on domain objects):
Task T008: "Implement HmacService in src/LabHMAC.Api/Application/HmacService.cs"
Task T010: "Configure Program.cs in src/LabHMAC.Api/Program.cs"
```

## Parallel Example: Phase 3 (User Story 1)

```text
# Launch unit tests in parallel (test-first):
Task T011: "Write unit tests for HmacService in tests/LabHMAC.Tests/Unit/HmacServiceTests.cs"
Task T012: "Write unit tests for HmacValidationResult in tests/LabHMAC.Tests/Unit/HmacValidationResultTests.cs"

# Then sequential (implementation):
Task T013: "Create HmacValidationFilter in src/LabHMAC.Api/Api/HmacValidationFilter.cs"
Task T014: "Create PaymentsController in src/LabHMAC.Api/Api/PaymentsController.cs"
Task T015: "Write integration test in tests/LabHMAC.Tests/Integration/PaymentsEndpointTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup → solution and projects created
2. Complete Phase 2: Foundational → domain + service + config ready
3. Complete Phase 3: User Story 1 → endpoint works, happy path tested
4. **STOP and VALIDATE**: `dotnet test` passes, manual test with curl/Postman returns 200 OK
5. MVP is ready — a developer can already see HMAC working

### Incremental Delivery

1. Setup + Foundational → project compiles and runs (empty)
2. Add User Story 1 → valid signed requests verified (MVP!)
3. Add User Story 2 → tampered request rejection verified
4. Add User Story 3 → missing/malformed header handling verified
5. Add Polish → simulator makes it demo-ready, end-to-end validated
6. Each increment adds educational value without breaking previous scenarios

---

## Notes

- All HMAC-related classes include XML doc comments (FR-008) — embedded in implementation tasks, not separate
- Debug logging for HMAC steps (FR-006) — embedded in T008 (HmacService implementation)
- Secret key loaded from IConfiguration (FR-004) — embedded in T008 and T009
- Timing-safe comparison (FR-005) — embedded in T008 via CryptographicOperations.FixedTimeEquals
- WebApplicationFactory used for all integration tests (research.md decision)
