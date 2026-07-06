# Inventory Hold Microservice

Inventory Hold Microservice — A .NET 10 service that places temporary holds on inventory items during checkout, preventing overselling under concurrent load.

## Prerequisites

- Docker Desktop
- .NET 10 SDK (for local development and build)
- Node 20 (for local frontend development)

> Docker is the only requirement to run the full stack.

## Quickstart

```bash
git clone <repo-url>
cd <repo-folder>
docker-compose up --build
```

Open the application:

- Frontend: `http://localhost:3000`
- API Swagger: `http://localhost:5000/swagger`
- RabbitMQ UI: `http://localhost:15672` (guest/guest)

## Running Tests

```bash
dotnet test
```

## API Reference

### POST /api/holds
Creates a new hold.

Request body:

```json
{
  "productId": "string",
  "quantity": 1,
  "customerId": "string"
}
```

Success: `201 Created`

Error responses:

- `400` `VALIDATION_ERROR` for invalid payload
- `409` `INSUFFICIENT_STOCK` when stock is unavailable
- `500` `INTERNAL_ERROR` for unexpected errors

### GET /api/holds/{holdId}
Retrieves an existing hold.

Success: `200 OK`

Error responses:

- `404` `HOLD_NOT_FOUND`
- `500` `INTERNAL_ERROR`

### DELETE /api/holds/{holdId}
Releases an active hold.

Success: `200 OK`

Error responses:

- `404` `HOLD_NOT_FOUND`
- `409` `ALREADY_RELEASED` when the hold is already released
- `409` `HOLD_EXPIRED` when the hold has already expired
- `500` `INTERNAL_ERROR`

### GET /api/inventory
Returns current inventory.

Success: `200 OK`

Error responses:

- `500` `INTERNAL_ERROR`

## Architecture Overview

This service is built with clear DDD layers:

- `Contracts` for DTOs, enums, and events
- `Domain` for entities, domain rules, and service behavior
- `Infrastructure` for MongoDB, Redis, RabbitMQ, and background services
- `WebApi` for HTTP controllers and startup wiring

`availableStock` is stored as a denormalized MongoDB field so atomic stock deduction can use an indexed `Gte` filter instead of `$expr`.

## Key Design Decisions

- Atomic stock deduction uses MongoDB `FindOneAndUpdateAsync` with `Gte(AvailableStock, quantity)`.
- `AvailableStock` is a stored field to keep the deduction query indexable.
- Hold expiry is handled lazily on read plus a periodic cleanup service.
- Redis cache invalidation is triggered on hold creation, release, and expiry.
- RabbitMQ uses a topic exchange with routing keys `hold.created`, `hold.released`, and `hold.expired`.

## Configuration Reference

| Environment Variable | `appsettings.json` Key | Default | Description |
|----------------------|------------------------|---------|-------------|
| `MongoDB__ConnectionString` | `MongoDB:ConnectionString` | `mongodb://localhost:27017` | MongoDB connection string |
| `MongoDB__DatabaseName` | `MongoDB:DatabaseName` | `inventoryhold` | MongoDB database name |
| `REDIS__ConnectionString` | `Redis:ConnectionString` | `localhost:6379,abortConnect=false` | Redis connection string |
| `RABBITMQ__Host` | `RabbitMQ:Host` | `localhost` | RabbitMQ hostname |
| `RABBITMQ__Port` | `RabbitMQ:Port` | `5672` | RabbitMQ port |
| `RABBITMQ__Username` | `RabbitMQ:Username` | `guest` | RabbitMQ username |
| `RABBITMQ__Password` | `RabbitMQ:Password` | `guest` | RabbitMQ password |
| `RABBITMQ__VirtualHost` | `RabbitMQ:VirtualHost` | `/` | RabbitMQ virtual host |
| `RABBITMQ__ExchangeName` | `RabbitMQ:ExchangeName` | `inventory-hold.events` | RabbitMQ exchange name |
| `HOLD__DefaultTtlSeconds` | `Hold:DefaultTtlSeconds` | `900` | Default hold TTL in seconds |

## Project Structure

- `src/InventoryHold.Contracts/` — API contracts, request/response DTOs, event records
- `src/InventoryHold.Domain/` — domain entities, exceptions, repository interfaces, service logic
- `src/InventoryHold.Infrastructure/` — MongoDB, Redis, RabbitMQ, DI wiring, background services
- `src/InventoryHold.WebApi/` — ASP.NET Core controllers, startup, error handling
- `src/InventoryHold.UnitTests/` — xUnit unit tests for domain and service logic
- `frontend/` — React SPA built with Vite and TanStack Query
- `docker-compose.yml` — local multi-container orchestration
- `docs/` — architecture and review artifacts
