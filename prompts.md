# AI Prompts Used During Development

Tool used: Claude Code (IDE-integrated).

# 1. Requirements Analysis & Architecture Design

**Prompt**

Read this case study brief attatched document. Summarize the functional and non-functional requirements, and flag anything ambiguous I should decide on before writing spec.md. Then propose a simple, layered architecture for a .NET 8 Minimal API solution (Models, Interfaces, Providers, Services, and Minimal API endpoint mapping — not Controllers) that satisfies these requirements, and explain why each layer exists.

---

**Purpose**

Used to turn the challenge brief into a requirements summary, a list of ambiguities to decide before writing spec.md, and a layered architecture proposal (Models/Interfaces/Providers/Services/Endpoints) to guide implementation.

# 2. Spec Authoring

**Prompt**

Write spec.md for this project — unified status model, provider contracts, merge rules,
API contract (GET /flights/status), and a table of deterministic test scenarios covering
every status plus single-provider-only and no-provider cases.

---

**Purpose**

Used to produce spec.md: the unified status enum and reference-time-selection rule, AeroTrack/QuickFlight raw contracts and status vocabularies, the merge rules with tie-break, the GET /flights/status API contract, and a 15-row deterministic scenario table for later test fixtures. Note: the brief left several rules ambiguous (provider vocabularies, departure-vs-arrival precedence, tie-break on equal lastUpdatedUtc, null-vs-Unknown distinction); I resolved these myself and recorded each decision in spec.md's Assumptions & Design Decisions section rather than leaving them implicit.

# 3. Models and IFlightStatusProvider Generation

**Prompt**

Generate the models and IFlightStatusProvider interface based on spec.md.

---

**Purpose**

Used to scaffold FlightStatus.Api/Models (UnifiedFlightStatus, ProviderFlightStatus, FlightStatusResult, AeroTrackResponse, QuickFlightResponse) and FlightStatus.Api/Interfaces/IFlightStatusProvider directly from the field lists and contracts already committed to spec.md.

# 4. Provider Stub Generation

**Prompt**

Generate two deterministic provider stubs, AeroTrackProvider and QuickFlightProvider,
implementing IFlightStatusProvider per spec.md — cover every scenario in the table.

---

**Purpose**

Used to generate FlightStatus.Api/Providers/AeroTrackProvider.cs and QuickFlightProvider.cs: fixed in-memory fixture dictionaries keyed by (flightNumber, date) implementing each provider's status-vocabulary normalization and AeroTrack's reference-time-selection delay calculation, with fixture coverage mapped to all 15 rows of spec.md's Deterministic Test Scenarios table.

# 5. Status Normalization Check

**Prompt**

Add status normalization per the mapping table in spec.md.

---

**Purpose**

Requested to add status normalization; verified it was already implemented in AeroTrackProvider.Normalize and QuickFlightProvider.Normalize as part of the prior provider-stub generation, and confirmed both match spec.md's vocabulary tables verbatim. Note: no new code was added — the request was satisfied by pointing to the existing implementation rather than duplicating it.

# 6. FlightStatusService Generation

**Prompt**

Generate FlightStatusService — query providers in parallel, normalize each result, then
apply the merge rules from spec.md.

---

**Purpose**

Used to generate FlightStatus.Api/Interfaces/IFlightStatusService.cs and FlightStatus.Api/Services/FlightStatusService.cs: queries all injected IFlightStatusProvider instances in parallel via Task.WhenAll and applies spec.md's three merge rules (both respond → later LastUpdatedUtc wins with AeroTrack tie-break; one responds → use it; neither responds → synthetic Unknown with message). Note: FlightStatusResult's ScheduledDeparture/ScheduledArrival are non-nullable per spec.md, so I used DateTime.MinValue as a sentinel for the neither-responds case rather than changing the contract — callers should key off Message being non-null to detect that case, not the schedule fields.

# 7. Minimal API Wiring

**Prompt**

Wire this up as a .NET 8 Minimal API in Program.cs — not Controllers — with the
/flights/status endpoint per spec.md, DI-registered providers and services, and 400s for
missing/invalid query params.

---

**Purpose**

Used to generate FlightStatus.Api/Endpoints/FlightStatusEndpoints.cs (a MapGet extension method validating flightNumber/date presence and yyyy-MM-dd format, returning Results.Problem for 400s) and wire DI registration for both IFlightStatusProvider implementations and IFlightStatusService into Program.cs, then ran the API and curled every merge scenario plus all three 400 cases to confirm end-to-end behavior. Note: verified UnifiedFlightStatus serializes as a numeric enum by default rather than a string, and flagged it to the user as an open decision (add JsonStringEnumConverter now vs. handle it on the frontend) rather than deciding unilaterally.

# 8. Global Exception Handling

**Prompt**

Add global exception handling so unhandled exceptions return a consistent JSON 500 instead
of a stack trace.

---

**Purpose**

Used to add app.UseExceptionHandler(...) in Program.cs, writing a consistent application/problem+json 500 body via IProblemDetailsService instead of a stack trace or dev exception page, then verified it live by temporarily throwing from the endpoint for a sentinel flight number and reverting afterward. Note: ProblemDetails for this purpose actually resolves from Microsoft.AspNetCore.Mvc rather than Microsoft.AspNetCore.Http as first assumed, despite IProblemDetailsService/ProblemDetailsContext living in Http — corrected via a using alias after a build error surfaced the right namespace.

# 9. xUnit Test Suite Generation

**Prompt**

Generate xUnit tests covering the normalization table, the merge scenarios (both respond,
one responds, neither responds, a provider throws), and the endpoint's validation (missing
params, malformed date).

---

**Purpose**

Used to generate the FlightStatus.Tests suite: AeroTrackProviderTests and QuickFlightProviderTests covering every vocabulary-table row and the 15-minute boundary, FlightStatusServiceTests covering all three merge rules plus the provider-throws case via a hand-rolled FakeFlightStatusProvider, and FlightStatusRequestValidatorTests covering missing/blank params and malformed dates; all 56 tests pass. Note: made Normalize/ComputeFromSchedule internal (with InternalsVisibleTo) and extracted endpoint validation into FlightStatusRequestValidator to make these testable without a WebApplicationFactory dependency, and changed FlightStatusService to catch a throwing provider and treat it as "no response" (merge rules 2/3) rather than letting the exception propagate to a 500 — this wasn't specified in spec.md, so I flagged it to the user as a behavior decision rather than silently encoding it as passing tests.