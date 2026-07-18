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

# 10. Frontend Implementation

**Prompt**

Build the frontend per the UI requirements in spec.md and the brief — search form, color-coded
status card, conditional AeroTrack-only fields, and a clear error state.

---

**Purpose**

Used to build flight-status-ui per spec.md's Frontend Design & Structure section: Tailwind CSS v4 wired via @tailwindcss/vite, services/flightStatusApi.ts, hooks/useFlightStatus.ts, and components/ (SearchForm, ResultCard, ErrorState, StatusBadge), then verified live by driving a real headless Chrome instance via CDP against both running dev servers — confirmed all five status colors, conditional AeroTrack fields, no scrollbar at 1280×900, no horizontal overflow at 375px, and the error state after killing the API mid-session. Note: this also required resolving the previously-flagged UnifiedFlightStatus serialization decision (added JsonStringEnumConverter in Program.cs so status serializes as a name, not an ordinal) since StatusBadge's color map needed to key off status names — updated spec.md's note to mark that decision resolved rather than leaving it stale.

# 11. README Authoring

**Prompt**

Write README.md — what it does, architecture, tech stack, setup/run steps for backend and
frontend from a clean clone, the API contract, key assumptions, how to run tests.

---

**Purpose**

Used to produce README.md: project summary, backend/frontend architecture overview cross-linked to spec.md, tech stack table, clean-clone setup/run steps for both apps, a table of known-good fixture flight numbers for manual testing, the API contract with a live example, a condensed summary of spec.md's key assumptions, and test-running instructions (dotnet test, a single-test filter example, and the frontend build/lint commands). Note: every command in the README (dotnet restore, dotnet run --project FlightStatus.Api --urls http://localhost:5299, npm install/npm run dev, the proxy, dotnet test --filter) was actually re-run from a cold-started state to confirm it works as written, not just transcribed from memory — the backend/frontend port-mismatch gotcha from earlier in this session is called out explicitly so it isn't rediscovered by a fresh clone.

# 12. Reflection Authoring

**Prompt**

Write reflection.md — what I'd improve with more time, and a closing "Lessons Learned"
section on where AI accelerated the work and where I had to override or correct its output.

---

**Purpose**

Used to produce reflection.md: a "What I'd improve with more time" list (frontend test coverage, integration tests through the real pipeline, fixture coverage tied to a single hardcoded date, CI, configurable API base URL, an accessibility pass, provider resiliency) and a "Lessons Learned" section split between where AI accelerated the work (spec-first ambiguity resolution, test generation from a written contract, full-stack scaffolding, catching its own bugs when asked to verify) and where output had to be overridden or corrected (the DateTime.MinValue sentinel, the unilateral provider-throws design decision, the color-scheme default that broke the date picker, the CSS Cascade Layers bug, the port-mismatch gap, and path-escaping bugs in debug tooling). Note: every incident cited is one that actually occurred earlier in this session, not a generic/invented example.