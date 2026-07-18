# Reflection

## What I'd improve with more time

1. **Frontend test coverage.** The backend has 56 xUnit tests; the frontend has none — correctness
   there was verified by driving the running app in a real browser rather than automated tests.
   I'd add Vitest + React Testing Library coverage for `useFlightStatus` (loading/data/error state
   transitions), `ResultCard`'s conditional rendering (AeroTrack-only fields, the no-schedule-data
   case), and `SearchForm`'s validation guard.

2. **Integration tests through the real pipeline.** `FlightStatusServiceTests` exercises the merge
   logic via a hand-rolled `FakeFlightStatusProvider`, and `FlightStatusRequestValidatorTests`
   exercises validation directly — but nothing spins up the actual Minimal API host and hits
   `GET /flights/status` end-to-end through routing, DI, JSON serialization, and the exception
   handler together. A `WebApplicationFactory`-based test class would close that gap.

3. **Fixture coverage tied to a single hardcoded date.** All 15 scenarios key off `2026-01-15`,
   which was the right call for deterministic tests but means anyone poking at the running app
   with "today's date" gets `Unknown` every time (this came up directly during manual testing).
   I'd add a small set of relative-date fixtures, or a documented "demo mode," so the app is
   discoverable without reading spec.md's scenario table first.

4. **CI.** No GitHub Actions workflow runs `dotnet test` / `npm run build` / `npm run lint` on
   push. Given the brief's emphasis on a clean, demoable submission, this would have been cheap
   to add and would have caught regressions automatically instead of relying on me remembering to
   re-run everything by hand each time.

5. **Configurable API base URL.** The frontend calls a relative `/flights/status` path and relies
   entirely on Vite's dev-server proxy to reach the backend. That's fine for local dev, but a real
   deployment would need an environment-configurable base URL (Vite env vars) rather than an
   assumption of same-origin/proxying.

6. **Accessibility pass.** The `DatePicker` popover handles outside-click and Escape, but I didn't
   do a focused keyboard-navigation/screen-reader pass on it or on the status badges' color-only
   signaling (color is paired with an icon and text label, which helps, but wasn't verified against
   an actual screen reader).

7. **Provider resiliency beyond "treat a throw as no response."** That's the only resiliency
   measure in place, and it was a design decision I accepted rather than one the brief asked for
   (see below). A production version talking to real providers would want real timeouts and
   possibly retries, not just exception-swallowing.

## Lessons learned

### Where AI accelerated the work

- **Turning an ambiguous brief into a committed contract.** Working through the case study brief
  surfaced every real ambiguity up front — provider vocabularies, departure-vs-arrival precedence,
  the merge tie-break, the null-vs-Unknown distinction — before any code existed. Every one of
  those became a documented decision in spec.md instead of an implicit assumption buried in code
  I'd have to reverse-engineer later.
- **Test generation from a written contract.** Once spec.md's vocabulary tables and 15-row
  scenario table existed, generating 56 xUnit tests that covered every row, both 15-minute
  boundary directions, and all three merge rules was fast and genuinely thorough — the kind of
  exhaustive-but-mechanical coverage that's easy to under-do by hand because it's tedious, not hard.
- **Full-stack scaffolding.** DI wiring, Minimal API endpoint mapping, `ProblemDetails` exception
  handling, and the entire React/Tailwind frontend went from nothing to working software in a
  handful of passes, each one actually run (not just written) before being reported done.
- **Catching its own mistakes when asked to verify.** Several bugs (the sentinel value leak, the
  missing date-picker icon, the CSS layering issue) were found and fixed within the same session
  because verification meant actually running the app and looking at screenshots or JSON output,
  not just re-reading the diff.

### Where I had to override or correct its output

- **A `DateTime.MinValue` sentinel instead of a nullable field.** The "neither provider responds"
  case initially encoded "no data" as `DateTime.MinValue` rather than `null` on
  `ScheduledDeparture`/`ScheduledArrival`. It worked, but it was a real API contract smell — a
  client could try to render `0001-01-01` — that I had to explicitly flag before it was corrected
  across the C# model, the merge service, spec.md, the TypeScript types, and the React component's
  render logic in one coordinated pass.
- **A design decision made without being asked.** Treating a throwing provider as "no response"
  (so one provider's failure doesn't fail the whole request) was not specified anywhere in the
  brief or spec.md — it was introduced unilaterally while writing tests for that exact scenario.
  It was flagged explicitly rather than silently shipped, which was the right instinct, but it's a
  behavior I had to consciously sign off on, not something that was obviously correct on its own.
- **A styling default that didn't match the actual design.** `color-scheme: light dark` was set
  globally early on — a "reasonable default" that turned out to actively break things: it made the
  native date-picker icon effectively invisible and caused a visible flash when the calendar
  opened, because the rest of the UI was light-only. This only surfaced through an actual bug
  report and browser screenshots, not code review.
- **A CSS cascade bug that looked right in the diff.** Re-theming `react-day-picker` to the app's
  slate palette via a Tailwind arbitrary-value class compiled fine and read as correct, but did
  nothing at runtime — Tailwind wraps utilities in `@layer`, and CSS Cascade Layers rules mean
  un-layered CSS (the library's own stylesheet) always wins regardless of source order. That's a
  non-obvious interaction that only a rendered screenshot exposed; the fix was moving the override
  to plain, un-layered CSS ordered after the library's stylesheet.
- **A port mismatch that only broke "for real."** The Vite proxy is hardcoded to port 5299, but
  running the API via a bare `dotnet run` (or an IDE's default run button) uses
  `launchSettings.json`'s port instead. Every test I'd run myself during development happened to
  use the right port explicitly; the mismatch only showed up when I ran the app the "normal" way
  afterward — a gap between how the AI verifies its own work and how a person actually uses the
  app day to day, now called out explicitly in the README so it isn't rediscovered.
- **Escaping bugs in its own throwaway tooling.** Several of the browser-automation debug scripts
  used to verify UI changes had recurring Windows path-escaping bugs (`\n`, `\c` silently
  swallowed as escape sequences instead of literal path separators). Harmless since they were
  disposable scripts, but a reminder that generated code — even test/debug scaffolding — needs the
  same scrutiny as application code, particularly around shell/JS string-escaping boundaries.
