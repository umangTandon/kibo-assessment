# Architecture Decision Record

## DDD Layering
The solution is split into four main layers:

- `InventoryHold.Contracts` contains immutable DTOs, request/response models, enums, and event contracts.
- `InventoryHold.Domain` contains core entities, domain exceptions, repository interfaces, service logic, and application options.
- `InventoryHold.Infrastructure` contains persistence, caching, messaging, background services, and DI wiring.
- `InventoryHold.WebApi` contains HTTP controllers, error handling, API startup configuration, and mapping between domain and contracts.

This separation ensures business logic is testable without infrastructure, keeps contracts stable for external consumers, and isolates implementation details behind interfaces.

## Atomic Stock Deduction
Inventory deduction must be race-safe under concurrent order creation. The implementation uses MongoDB `FindOneAndUpdateAsync` with a filter on `AvailableStock >= quantity` and an atomic `$inc` update.

The `AvailableStock` field is stored as a denormalized field in `InventoryDocument` rather than a computed property. This allows the query to use a standard index and avoid MongoDB `$expr`, which is not index-friendly and can degrade performance.

## Lazy Expiry
Hold expiry is detected lazily on read: `GetHoldAsync` checks whether an active hold has passed its `ExpiresAt`, transitions it to expired, restores inventory, and publishes a `HoldExpired` event.

A background cleanup service is also used as a safety net. This prevents reliance on MongoDB TTL indexes, which are not guaranteed to remove documents at a precise business-defined expiration moment.

## Redis Caching
Redis caches the inventory list and individual hold lookups to reduce load on MongoDB.

- `inventory:all` caches the inventory list for 5 minutes.
- `inventory:item:{id}` caches a single inventory item when needed.
- `hold:{holdId}` caches a hold until its actual expiration time.

Cache invalidation occurs on state-changing operations: hold creation, release, and expiry. This avoids stale data even when cache TTLs remain active.

## RabbitMQ Topology
The service publishes hold lifecycle events to a topic exchange named `inventory-hold.events`.

Routing keys:
- `hold.created`
- `hold.released`
- `hold.expired`

This topology makes it easy for consumers to subscribe to a specific event type or all hold events using `hold.*`.
