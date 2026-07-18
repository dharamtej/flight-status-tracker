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