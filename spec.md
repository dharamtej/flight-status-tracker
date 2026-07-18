# Flight Status Tracker — Spec

This document defines the data models, provider contracts, merge rules, API contract, and
deterministic test fixtures for the Flight Status lookup feature, before any implementation
exists. Where the challenge brief left a rule ambiguous, the decision taken is called out
explicitly in [Assumptions & Design Decisions](#assumptions--design-decisions).

All timestamps in this system are UTC (`DateTime` with `Kind = Utc` / ISO-8601 with `Z` suffix).

---

## Unified Status Model

```csharp
public enum UnifiedFlightStatus
{
    OnTime,
    Delayed,
    Cancelled,
    Diverted,
    Unknown
}
```

| Status | Rule |
|---|---|
| `OnTime` | Departure or arrival within 15 minutes of schedule |
| `Delayed` | Departure or arrival pushed beyond 15 minutes |
| `Cancelled` | Flight will not operate |
| `Diverted` | Flight landed at a different airport |
| `Unknown` | No usable status returned by either provider |

### Reference-time selection (for time-based computation)

`Cancelled`/`Diverted` are operational states independent of timing and take priority whenever a
provider reports them explicitly — no time math is applied in that case.

Otherwise, the on-time/delayed determination uses the first available of, in order:

1. `ActualArrival` vs `ScheduledArrival`, if `ActualArrival` is present.
2. `ActualDeparture` vs `ScheduledDeparture`, if `ActualDeparture` is present.
3. If neither actual time is present (flight hasn't reported movement yet), the flight is
   `OnTime` — there is no evidence of delay yet.

`|actual − scheduled| <= 15 minutes` → `OnTime`; `> 15 minutes` → `Delayed`.

---

## Provider Contracts

```csharp
public interface IFlightStatusProvider
{
    string ProviderName { get; }

    // Returns null when the provider has no data for this flight/date at all
    // (equivalent to "provider did not respond").
    Task<ProviderFlightStatus?> GetStatusAsync(string flightNumber, DateOnly date, CancellationToken ct);
}
```

### Normalized intermediate model — `ProviderFlightStatus`

Every provider implementation is responsible for translating its own raw vocabulary into this
shape before returning. This keeps vocabulary knowledge local to the provider that owns it.

```csharp
public sealed record ProviderFlightStatus(
    string ProviderName,
    UnifiedFlightStatus Status,
    DateTime ScheduledDeparture,
    DateTime? ActualDeparture,
    DateTime ScheduledArrival,
    DateTime? ActualArrival,
    string? Terminal,          // AeroTrack only
    string? Gate,              // AeroTrack only
    string? DelayReason,       // AeroTrack only
    DateTime LastUpdatedUtc
);
```

### AeroTrack (full detail)

Raw stub response shape:

```csharp
public sealed record AeroTrackResponse(
    string FlightNumber,
    string Status,              // see vocabulary below
    DateTime ScheduledDeparture,
    DateTime? ActualDeparture,
    DateTime ScheduledArrival,
    DateTime? ActualArrival,
    string? Terminal,
    string? Gate,
    string? DelayReason,
    DateTime LastUpdatedUtc
);
```

**Status vocabulary → normalization:**

| AeroTrack `Status` | Normalization |
|---|---|
| `CANCELLED` | `Cancelled` (terminal, no time calc) |
| `DIVERTED` | `Diverted` (terminal, no time calc) |
| `SCHEDULED`, `BOARDING`, `DEPARTED`, `LANDED`, `ON_TIME`, `DELAYED` | Computed via [reference-time selection](#reference-time-selection-for-time-based-computation) |
| anything unrecognized | `Unknown` |

### QuickFlight (minimal)

QuickFlight exposes no actual times, so it cannot be time-delta computed — it reports its own
precomputed state directly, which is mapped 1:1.

Raw stub response shape:

```csharp
public sealed record QuickFlightResponse(
    string FlightNumber,
    string FlightState,         // see vocabulary below
    DateTime ScheduledDeparture,
    DateTime ScheduledArrival,
    DateTime LastUpdatedUtc
);
```

**Status vocabulary → normalization:**

| QuickFlight `FlightState` | Normalization |
|---|---|
| `ONTIME` | `OnTime` |
| `DELAYED` | `Delayed` |
| `CANCELLED` | `Cancelled` |
| `DIVERTED` | `Diverted` |
| `UNKNOWN` / unrecognized | `Unknown` |

QuickFlight's `ProviderFlightStatus` always has `ActualDeparture = null`, `ActualArrival = null`,
`Terminal = null`, `Gate = null`, `DelayReason = null`.

---

## Merge Rules

Applied by `FlightStatusService` after both providers have been queried concurrently and each has
independently normalized its own response (or returned `null`):

1. **Both providers return a result** → take the one with the later `LastUpdatedUtc`. If equal,
   prefer AeroTrack (richer data source — explicit tie-break, see
   [Assumptions](#assumptions--design-decisions)).
2. **Only one provider returns a result** (the other is `null`) → use that result as-is.
3. **Neither provider returns a result** (`null` from both) → return `Unknown` with
   `Message = "No status available from either provider for flight {flightNumber} on {date}."`
   and all other fields empty/null.

Note: a provider returning a *non-null* result whose status is `Unknown` (unrecognized raw
vocabulary) is a valid "response" for rule 1/2 — it only falls to rule 3 when a provider returns
`null` outright (no data for that flight/date).

---

## API Contract

### `GET /flights/status`

**Query parameters**

| Name | Type | Required | Format |
|---|---|---|---|
| `flightNumber` | string | yes | non-empty |
| `date` | string | yes | `yyyy-MM-dd` |

**400 Bad Request** (`application/problem+json`) when `flightNumber` is missing/empty, `date` is
missing/empty, or `date` fails to parse as `yyyy-MM-dd`:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "flightNumber is required."
}
```

**200 OK** — `FlightStatusResult`:

```csharp
public sealed record FlightStatusResult(
    string FlightNumber,
    DateOnly Date,
    UnifiedFlightStatus Status,
    string Source,              // "AeroTrack" | "QuickFlight" | "None"
    DateTime ScheduledDeparture,
    DateTime? ActualDeparture,
    DateTime ScheduledArrival,
    DateTime? ActualArrival,
    string? Terminal,           // present only when Source == "AeroTrack" and provided
    string? Gate,                // present only when Source == "AeroTrack" and provided
    string? DelayReason,         // present only when Source == "AeroTrack" and provided
    DateTime? LastUpdatedUtc,    // null only in the neither-responds case
    string? Message              // populated only for the Unknown/neither-responds case
);
```

Always returns `200 OK` for a syntactically valid request, even when the resulting status is
`Unknown` — `Unknown` is a legitimate result, not an error.

---

## Deterministic Test Scenarios

All scenarios below use `date = 2026-01-15`. Each `flightNumber` is a fixed fixture key in the
respective stub provider — no randomness, no clock dependency.

| # | flightNumber | AeroTrack behavior | QuickFlight behavior | Expected `Status` | Expected `Source` | Covers |
|---|---|---|---|---|---|---|
| 1 | `SR100` | `ON_TIME`, actual arrival 5 min after scheduled | no fixture (`null`) | `OnTime` | AeroTrack | OnTime via time-calc, single-provider |
| 2 | `SR101` | no fixture (`null`) | `ONTIME` | `OnTime` | QuickFlight | OnTime via vocab, single-provider |
| 3 | `SR200` | `DELAYED`, actual departure 40 min after scheduled | no fixture (`null`) | `Delayed` | AeroTrack | Delayed via time-calc |
| 4 | `SR201` | no fixture (`null`) | `DELAYED` | `Delayed` | QuickFlight | Delayed via vocab |
| 5 | `SR300` | `CANCELLED` | no fixture (`null`) | `Cancelled` | AeroTrack | Cancelled, explicit, single-provider |
| 6 | `SR301` | no fixture (`null`) | `CANCELLED` | `Cancelled` | QuickFlight | Cancelled via vocab |
| 7 | `SR400` | `DIVERTED` | no fixture (`null`) | `Diverted` | AeroTrack | Diverted, explicit, single-provider |
| 8 | `SR401` | no fixture (`null`) | `DIVERTED` | `Diverted` | QuickFlight | Diverted via vocab |
| 9 | `SR500` | unrecognized raw status | unrecognized raw status | `Unknown` | AeroTrack or QuickFlight (later `LastUpdatedUtc`) | Unknown from unusable data, both respond |
| 10 | `SR600` | `ON_TIME`, `LastUpdatedUtc = 2026-01-15T08:00:00Z` | `DELAYED`, `LastUpdatedUtc = 2026-01-15T09:00:00Z` | `Delayed` | QuickFlight | Both respond, QuickFlight fresher wins |
| 11 | `SR601` | `DELAYED`, `LastUpdatedUtc = 2026-01-15T09:00:00Z` | `ONTIME`, `LastUpdatedUtc = 2026-01-15T08:00:00Z` | `Delayed` | AeroTrack | Both respond, AeroTrack fresher wins |
| 12 | `SR602` | `ON_TIME`, `LastUpdatedUtc = 2026-01-15T08:00:00Z` | `ONTIME`, `LastUpdatedUtc = 2026-01-15T08:00:00Z` | `OnTime` | AeroTrack | Equal `LastUpdatedUtc` tie-break → AeroTrack |
| 13 | `SR700` | `ON_TIME`, includes `Terminal`, `Gate` | no fixture (`null`) | `OnTime` | AeroTrack | AeroTrack-only fields (terminal/gate) populated |
| 14 | `SR800` | no fixture (`null`) | no fixture (`null`) | `Unknown` | `None` | Neither responds — synthetic Unknown + message |
| 15 | `SR900` | `DELAYED`, `DelayReason = "WEATHER"` | no fixture (`null`) | `Delayed` | AeroTrack | AeroTrack-only field (delay reason) populated |

---

## Assumptions & Design Decisions

These resolve ambiguities in the original brief; documented here so they're auditable rather than
buried in code.

1. **Provider vocabularies are invented** — the brief specifies detail level but not literal
   status strings. See vocabulary tables above; these are the full contract, not just examples.
2. **Departure vs. arrival**: use arrival if an actual arrival time exists, else departure if an
   actual departure time exists, else assume `OnTime` (no evidence of delay yet). See
   [Reference-time selection](#reference-time-selection-for-time-based-computation).
3. **Explicit Cancelled/Diverted status always overrides time-based computation** within a single
   provider's own normalization — these are operational states, not timing states.
4. **Tie-break on equal `LastUpdatedUtc`**: AeroTrack wins, since it's the richer data source.
5. **"No response" is modeled as the provider method returning `null`** for a flight/date it has
   no fixture for. This is distinct from a provider returning a real (non-null) result with
   `Status = Unknown` because its raw vocabulary was unrecognized — both are "a response", only
   the former triggers merge rule 3.
6. **All timestamps are UTC.** No timezone conversion is performed anywhere in this system.
7. **Error contract uses RFC7807 `ProblemDetails`** (ASP.NET Core's built-in default) for the 400
   case, rather than a bespoke error envelope.
8. **A syntactically valid request always returns `200 OK`**, even for the `Unknown` case —
   `Unknown` is data, not a server error.
