# AI Usage

## AI Tools Used

| Tool | How Used |
|------|----------|
| GitHub Copilot Chat (`@workspace`) | Primary AI tool. Ran all 10 specialist agents via `.github/agents/` instruction files. Each agent received scoped context and produced a specific layer of the codebase. |
| GitHub Copilot (inline) | Used during development for code completions, especially in infrastructure wiring (DI registrations, MongoDB filter expressions, RabbitMQ channel setup). |
| `.github/copilot-instructions.md` | Root context file auto-loaded by Copilot for every chat session. Defined tech stack, global rules, and pipeline index so every agent started with consistent constraints. |

## Copilot Agent Pipeline Strategy

The project was structured as a sequential 10-agent pipeline governed by an Orchestrator:

- **Why a pipeline?** Each layer depends on the previous one — the architecture must exist before business logic, and the infrastructure must exist before wiring it into the API.
- **Agent files as instructions** — each `.github/agents/NN-name.md` file scoped the task, input files, and expected outputs for that layer.
- **Orchestrator (Agent 00)** — managed the sequence, enforced quality gates, and decided whether to advance or retry.
- **Handoff messages** — every agent ended with a clear handoff to the next agent, which passed the exact files and state needed.
- **Narrow scope per agent** — each agent owned specific files only, reducing cross-layer interference and ensuring separation of concerns.

## Accepted Copilot Suggestions

- **`IExceptionHandler` for domain error mapping** — Copilot proposed using ASP.NET Core's `IExceptionHandler` interface rather than middleware. Accepted because it is composable and keeps controllers free of try/catch.
- **TanStack Query v5 for frontend server state** — Copilot recommended using TanStack Query instead of manual `useEffect`/`useState` fetching. Accepted because `invalidateQueries` after mutations directly solves the requirement that inventory numbers update immediately after a hold is placed or released.
- **`IServiceScopeFactory` in `ExpiredHoldCleanupService`** — Copilot used scope factory injection in the background service rather than injecting scoped services directly. Accepted because injecting scoped services into a singleton `BackgroundService` causes a captive dependency bug at runtime.
- **`NullLogger<T>` in unit tests** — Copilot used `NullLogger<HoldService>.Instance` instead of mocking `ILogger<HoldService>`. Accepted because logger behavior is side-effect free and does not need to be asserted in these unit tests.

## Rejected Copilot Suggestions

- **MongoDB TTL index for hold expiry** — Rejected because TTL index cleanup is not guaranteed to fire at the exact business expiry moment, so the service uses lazy expiry detection and a cleanup background service instead.
- **`availableStock` as a computed property** — Rejected because computed field comparisons require MongoDB `$expr`, which is not index-friendly. Storing `AvailableStock` as a real field keeps the atomic deduction query efficient.
- **`npm ci` in frontend Dockerfile** — Rejected because a lockfile may not exist in the initial repo state; `npm install` is safer when package-lock.json is not guaranteed.
- **Separate `AvailableStock` read after deduction** — Rejected because `FindOneAndUpdateAsync` with `ReturnDocument.After` already returns the updated document atomically; a second read is unnecessary and creates a TOCTOU window.

## Test Validation

- Agent 08 (Test Engineer) was instructed to write a complete xUnit test suite and prepare the project for execution. Validation is pending where `dotnet` is unavailable in the current terminal environment.
- Common validation fixes:
  - Default cache setup returned `null` for all cache keys, then tests that needed a cache hit overrode that behavior for the specific key.
  - The `ExpiredButActiveStatusHold()` fixture uses `nameof(Hold.ExpiresAt)` to avoid brittle reflection.
  - The domain project includes `[assembly: InternalsVisibleTo("InventoryHold.UnitTests")]` so test fixtures can initialize private constructors safely.
- Agent 09 (Code Reviewer) reviewed the source and confirmed the code is structured for a final `dotnet test` pass once the runtime is available.

## Lessons Learned

Using an orchestrated agent pipeline works well when each layer has a narrow, predictable scope. It is important to specify low-level implementation constraints up front for infrastructure-heavy requirements like atomic MongoDB updates and RabbitMQ async APIs.
