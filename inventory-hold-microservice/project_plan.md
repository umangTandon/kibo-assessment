# Inventory Hold Microservice Project Plan

## 1. Project Summary

The Inventory Hold Microservice provides an e-commerce checkout reservation system that temporarily holds items while a customer completes payment. It is used by checkout workflows to prevent overselling, keep inventory accurate under concurrency, and automatically release or expire holds when they are no longer valid.

## 2. Functional Requirements

FR-01: The system shall atomically deduct inventory when a hold is created, preventing overselling under concurrent requests.
FR-02: Holds shall expire after a configurable TTL (default: 15 minutes) and automatically restore inventory when expired.
FR-03: The system shall allow creation of a hold with productId, customerId, and quantity, returning a unique holdId.
FR-04: The system shall expose an endpoint to retrieve an existing hold by holdId and return its status and remaining time.
FR-05: The system shall allow releasing a hold before expiry, restoring inventory immediately and publishing a HoldReleased event.
FR-06: The system shall seed inventory on startup if missing and expose a read-only inventory list endpoint.
FR-07: The system shall publish RabbitMQ events for HoldCreated, HoldReleased, and HoldExpired with a stable contract.
FR-08: The system shall cache inventory and hold lookups in Redis, returning cache misses as null and invalidating affected entries after state changes.
FR-09: The API shall return clear error responses for invalid requests, not-found holds, insufficient stock, and internal failures using proper HTTP status codes.
FR-10: The frontend shall display inventory, allow creating a hold, show active holds, and update inventory numbers immediately after hold actions.

## 3. Non-Functional Requirements

NFR-01: Stock deduction must be safe from race conditions and must not rely on application-level locks or optimistic retry loops.
NFR-02: The entire environment must start with a single `docker-compose up --build` command.
NFR-03: No hardcoded connection strings may exist in source code; all connections must come from `appsettings.json` or environment variables.
NFR-04: Unit tests must be isolated from infrastructure and must not use real MongoDB, Redis, or RabbitMQ clients.
NFR-05: The service must use .NET 10 with nullable reference types enabled.
NFR-06: The frontend must not hardcode `localhost` and must use service hostnames suitable for Docker networking.

## 4. Technology Decisions

| Technology | Version | Reason |
|------------|---------|--------|
| .NET | 10 / C# 14 | Modern LTS runtime with strong API and container support |
| MongoDB | 7 | Atomic document updates enable safe inventory deduction without locks |
| Redis | StackExchange.Redis 3.x | Fast cache store with .NET compatibility and async support |
| RabbitMQ | Client 7.x | Reliable messaging with async publish API and exchange routing |
| xUnit | 8.x | Standard .NET testing framework for service and domain tests |
| React | 18 | Modern SPA framework with stable hooks and ecosystem |
| TanStack Query | v5 | Server state management for frontend caching and invalidation |
| Docker | Compose | Standard multi-service orchestration for local development |

## 5. Domain Model

- **Hold**: `HoldId`, `ProductId`, `CustomerId`, `Quantity`, `Status` (Active, Released, Expired), `CreatedAt`, `ExpiresAt`, `ReleasedAt`, `Version`
- **InventoryItem**: `ProductId`, `ProductName`, `TotalStock`, `ReservedStock`, `AvailableStock`, `Version`

Storing `AvailableStock` as a real field rather than a computed property enables atomic MongoDB filters such as `AvailableStock >= quantity` without requiring `$expr`, which is necessary for efficient, index-backed deduction queries.

## 6. API Contract

### POST /api/holds
Request body: `{ "productId": "string", "customerId": "string", "quantity": int }`
Success: `201 Created` with `{ "holdId": "string", "status": "Active", "expiresAt": "datetime" }`
Errors:
- `400 Bad Request` for invalid payload or quantity <= 0
- `409 Conflict` if insufficient stock
- `500 Internal Server Error` for unexpected failures

### GET /api/holds/{holdId}
Success: `200 OK` with `{ "holdId": "string", "productId": "string", "customerId": "string", "quantity": int, "status": "Active|Released|Expired", "createdAt": "datetime", "expiresAt": "datetime", "releasedAt": "datetime?" }`
Errors:
- `404 Not Found` if holdId does not exist
- `500 Internal Server Error` for unexpected failures

### DELETE /api/holds/{holdId}
Success: `200 OK` with hold response reflecting `Released` status
Errors:
- `404 Not Found` if holdId does not exist
- `400 Bad Request` if hold is already expired or released
- `500 Internal Server Error` for unexpected failures

### GET /api/inventory
Success: `200 OK` with `[ { "productId": "string", "productName": "string", "availableStock": int, "totalStock": int } ]`
Errors:
- `500 Internal Server Error` for unexpected failures

## 7. Event Contracts

### HoldCreated
- Exchange: `inventory-hold.events`
- Routing key: `hold.created`
- Payload:
  - `HoldId`: string
  - `ProductId`: string
  - `CustomerId`: string
  - `Quantity`: int
  - `Status`: string
  - `CreatedAt`: datetime
  - `ExpiresAt`: datetime

### HoldReleased
- Exchange: `inventory-hold.events`
- Routing key: `hold.released`
- Payload:
  - `HoldId`: string
  - `ProductId`: string
  - `CustomerId`: string
  - `Quantity`: int
  - `ReleasedAt`: datetime
  - `Reason`: string

### HoldExpired
- Exchange: `inventory-hold.events`
- Routing key: `hold.expired`
- Payload:
  - `HoldId`: string
  - `ProductId`: string
  - `Quantity`: int
  - `ExpiredAt`: datetime
  - `Reason`: string

## 8. Agent Work Breakdown

| Agent | Deliverables |
|-------|--------------|
| 02 Architect | Create `InventoryHold.sln`, `NuGet.Config`, and solution/project scaffolding for Contracts, Domain, Infrastructure, WebApi, UnitTests; define architecture decisions and docs/architecture.md |
| 03 Backend Developer | Implement `Hold` entity, service layer, contracts, controllers, error handling, and domain interfaces; ensure clean separation from infrastructure |
| 04 MongoDB Specialist | Implement MongoDB repositories, atomic inventory deduction, seeding, persistence models, and infrastructure DI wiring |
| 05 Redis Specialist | Implement Redis caching layer, cache key helpers, cache service, and register Redis dependencies |
| 06 RabbitMQ Specialist | Implement RabbitMQ publisher, topology initializer, background cleanup service, and publish events on state changes |
| 07 Frontend Developer | Build React SPA with inventory dashboard, hold creation form, active holds list, Dockerized frontend, and API integration without localhost strings |
| 08 Test Engineer | Build xUnit test suite with fixtures and unit tests for domain, service, and API behavior; verify infrastructure is mocked out |
| 09 Code Reviewer | Produce `docs/review-report.md`, validate builds/tests, fix issues, and ensure no hardcoded credentials |
| 10 Documentation Writer | Produce `README.md` and `AI-USAGE.md` with quickstart, API reference, architecture summary, and Copilot agent pipeline explanation |

## 9. Risk Register

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| RabbitMQ connection failure at startup | Medium | High | Use retry/backoff and health checks; fail startup if missing and provide clear logs |
| MongoDB index missing causing slow expiry or stock check | Medium | Medium | Define proper indexes on `AvailableStock`, `ExpiresAt`, and `ProductId` in Infrastructure seeding |
| Redis serialization mismatch between cache and service models | Low | Medium | Use explicit typed JSON serialization with stable contract helpers |
| Expired hold not restoring inventory due to missed background scan | Medium | High | Implement lazy expiry on read plus periodic cleanup background service |
| API error responses inconsistent across controllers | Medium | Medium | Use centralized domain exception handler and standardized error DTOs |

## 10. Definition of Done

- [ ] `docker-compose up --build` starts all services without errors
- [ ] All 4 API endpoints return correct status codes for success and failures
- [ ] Inventory levels update after create/release without requiring page refresh
- [ ] `dotnet test` passes with at least 7 tests in `InventoryHold.UnitTests`
- [ ] No hardcoded connection strings in source code
- [ ] RabbitMQ management UI shows messages on `inventory-hold.events` exchange for hold events
- [ ] `README.md` documents quickstart, API reference, and architecture
- [ ] `AI-USAGE.md` documents AI strategy, accepted/rejected suggestions, and test validation
- [ ] `docs/architecture.md` explains `availableStock` denormalization and atomic deduction approach

## Self-Review Checklist

- [x] `project_plan.md` exists at solution root
- [x] All 10 functional requirements are numbered and testable
- [x] The domain model explains the `availableStock` denormalization decision
- [x] All 3 event payloads are fully defined with field names and types
- [x] The Definition of Done is a concrete checklist
- [x] The Agent Work Breakdown table covers agents 02–10 with no overlaps

## Handoff

Planning complete. `project_plan.md` is at the solution root. Read it before designing the solution structure.