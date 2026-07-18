# Flight Status Tracker

A Flight Status lookup tool for the SkyRoute platform. A support agent enters a flight number and
a date; the system queries two independent (stubbed) flight data providers, normalizes their
responses into a single unified status model, merges them per a documented set of rules, and
displays the result.

No real flight APIs, credentials, or persistence are involved — everything runs fully offline
against deterministic in-memory fixtures.

---

## Architecture

**Backend** — `FlightStatus.Api`, a .NET Minimal API (no Controllers), layered as:

```
Models/       UnifiedFlightStatus, ProviderFlightStatus, FlightStatusResult,
              AeroTrackResponse, QuickFlightResponse
Interfaces/   IFlightStatusProvider, IFlightStatusService
Providers/    AeroTrackProvider, QuickFlightProvider
              — deterministic stubs implementing IFlightStatusProvider, each owning its
                own raw status vocabulary and normalizing it to UnifiedFlightStatus
Services/     FlightStatusService
              — queries all registered providers concurrently, applies the merge rules
Endpoints/    FlightStatusEndpoints, FlightStatusRequestValidator
              — thin MapGet handler; validation and business logic live elsewhere
Program.cs    DI wiring + global exception handling (JSON ProblemDetails, never a stack trace)
```

Providers are registered against `IFlightStatusProvider` and injected as a collection, so
`FlightStatusService` doesn't know or care how many providers exist — adding a third provider
touches only `Program.cs` and a new `Providers/*.cs` file.

**Frontend** — `flight-status-ui`, a single-page React + TypeScript app:

```
components/   SearchForm, ResultCard, ErrorState, StatusBadge, DatePicker
hooks/        useFlightStatus — encapsulates the API call + loading/data/error state
services/     flightStatusApi — fetch logic + response types for GET /flights/status
```

Full data contracts, provider vocabularies, the unified status rules, the merge rules, and the
frontend design rules are documented in **[spec.md](spec.md)** — write that down before touching
this section if anything here seems to disagree with it; spec.md is the source of truth.

## Tech stack

| | |
|---|---|
| Backend | .NET 9 Minimal API (C#), satisfies the "`.NET 8+`" requirement |
| Frontend | React 19 + TypeScript, Vite 8, Tailwind CSS v4, react-day-picker |
| Tests | xUnit (backend) |

---

## Setup & run (from a clean clone)

**Prerequisites:** .NET 9 SDK (or 8+), Node.js 18+ and npm.

### 1. Backend

```bash
dotnet restore
dotnet run --project FlightStatus.Api --urls http://localhost:5299
```

> **Important:** always pass `--urls http://localhost:5299` explicitly. The frontend's dev
> server proxies `/flights/*` requests to `http://localhost:5299` (see
> `flight-status-ui/vite.config.ts`). Running the API via `dotnet run` with no flags, or via an
> IDE's default run button, uses `Properties/launchSettings.json`'s port (`5250`/`7178`) instead
> — the proxy will then fail with `ECONNREFUSED`.

Verify it's up:

```bash
curl "http://localhost:5299/flights/status?flightNumber=SR100&date=2026-01-15"
```

### 2. Frontend

In a second terminal, with the backend still running:

```bash
cd flight-status-ui
npm install
npm run dev
```

Open **http://localhost:5173**.

### Trying it out

The two providers are deterministic in-memory fixtures, not random data — most flight
number/date combinations correctly return `Unknown` ("no status available"), which is expected
behavior, not a bug. **Every fixture below is dated 15 Jan 2026** — any other date returns
`Unknown` for any flight number, since no fixture exists for it.

| Flight number | Date | Expected result |
|---|---|---|
| `SR100` | 15 Jan 2026 | 🟢 On Time (AeroTrack) |
| `SR101` | 15 Jan 2026 | 🟢 On Time (QuickFlight) |
| `SR200` | 15 Jan 2026 | 🟡 Delayed (AeroTrack) |
| `SR201` | 15 Jan 2026 | 🟡 Delayed (QuickFlight) |
| `SR300` | 15 Jan 2026 | 🔴 Cancelled (AeroTrack) |
| `SR301` | 15 Jan 2026 | 🔴 Cancelled (QuickFlight) |
| `SR400` | 15 Jan 2026 | 🔴 Diverted (AeroTrack) |
| `SR401` | 15 Jan 2026 | 🔴 Diverted (QuickFlight) |
| `SR500` | 15 Jan 2026 | ⚪ Unknown (both respond, unrecognized status) |
| `SR600` | 15 Jan 2026 | 🟡 Delayed (both respond, QuickFlight fresher wins) |
| `SR601` | 15 Jan 2026 | 🟡 Delayed (both respond, AeroTrack fresher wins) |
| `SR602` | 15 Jan 2026 | 🟢 On Time (equal timestamp, AeroTrack tie-break) |
| `SR700` | 15 Jan 2026 | 🟢 On Time, shows Terminal/Gate |
| `SR800` | 15 Jan 2026 | ⚪ Unknown (neither provider responds) |
| `SR900` | 15 Jan 2026 | 🟡 Delayed, shows Delay reason |

This is the same table as spec.md's
[Deterministic Test Scenarios](spec.md#deterministic-test-scenarios) section — see there for the
full per-scenario provider behavior (raw status, timestamps) behind each row.

---

## API contract

### `GET /flights/status?flightNumber={code}&date={yyyy-MM-dd}`

- **400 Bad Request** (`application/problem+json`) if `flightNumber` or `date` is missing, or
  `date` isn't in `yyyy-MM-dd` format.
- **200 OK** with a `FlightStatusResult` JSON body — always, even when the resulting status is
  `Unknown`; that's a legitimate result, not an error.

```bash
curl "http://localhost:5299/flights/status?flightNumber=SR900&date=2026-01-15"
```

```json
{
  "flightNumber": "SR900",
  "date": "2026-01-15",
  "status": "Delayed",
  "source": "AeroTrack",
  "scheduledDeparture": "2026-01-15T10:00:00Z",
  "actualDeparture": "2026-01-15T10:40:00Z",
  "scheduledArrival": "2026-01-15T12:00:00Z",
  "actualArrival": null,
  "terminal": null,
  "gate": null,
  "delayReason": "WEATHER",
  "lastUpdatedUtc": "2026-01-15T10:40:00Z",
  "message": null
}
```

`status` serializes as its name (`"OnTime"`, `"Delayed"`, …), not a numeric enum.
`scheduledDeparture`/`scheduledArrival` are `null` only in the neither-provider-responds case,
alongside a populated `message`. Full field-by-field contract: spec.md's
[API Contract](spec.md#api-contract) section.

---

## Key assumptions

The challenge brief left several rules ambiguous; the resolutions taken (and why) are fully
documented in spec.md's [Assumptions & Design Decisions](spec.md#assumptions--design-decisions).
Summary:

- Provider status vocabularies (e.g. AeroTrack's `ON_TIME`/`DELAYED`/…) are invented — the brief
  specified detail level, not literal strings.
- On-time/delayed is computed from actual arrival time if present, else actual departure time,
  else assumed on-time (no evidence of delay yet).
- Explicit `Cancelled`/`Diverted` from a provider always overrides time-based computation.
- On a merge tie (equal `lastUpdatedUtc` from both providers), AeroTrack wins.
- A provider "not responding" is distinct from it returning `Unknown` from unrecognized data —
  the former can happen because no fixture exists for that flight/date, or because the provider
  threw (both are treated identically by the merge logic).
- All timestamps are UTC; no timezone conversion happens anywhere in the system.

---

## Running tests

### Backend

```bash
dotnet test
```

56 xUnit tests covering: the full AeroTrack/QuickFlight status-vocabulary tables, the 15-minute
on-time/delayed boundary, all three merge rules (both respond / one responds / neither responds)
including the provider-throws case, and the endpoint's request validation (missing params,
malformed date).

Run a single test:

```bash
dotnet test --filter "FullyQualifiedName~FlightStatusServiceTests.GetStatusAsync_BothRespond_LaterLastUpdatedUtcWins"
```

### Frontend

```bash
cd flight-status-ui
npm run build   # tsc -b && vite build — typechecks and bundles
npm run lint    # oxlint
```

There is no separate frontend unit test suite; frontend correctness was verified by driving the
running app in a real browser (see prompts.md for what was checked).

---

## Repository layout

```
flight-status-tracker/
├── README.md            this file
├── spec.md              data models, contracts, merge rules, frontend design — source of truth
├── prompts.md           AI prompts used during development, with notes on decisions
├── reflection.md        what would be improved with more time
├── FlightStatus.Api/    backend (.NET Minimal API)
├── FlightStatus.Tests/  backend tests (xUnit)
└── flight-status-ui/    frontend (React + TypeScript + Vite)
```
