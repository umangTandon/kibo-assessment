# Review Report

## Critical Issues

[CRITICAL] File: src/InventoryHold.WebApi/Program.cs, Line: ~12
Issue: `AddInfrastructure` was not called, so MongoDB, Redis, and RabbitMQ registration would be missing.
Fix: Added `builder.Services.AddInfrastructure(builder.Configuration);` before registering controllers.

[CRITICAL] File: src/InventoryHold.Domain/Services/HoldService.cs, Line: ~80
Issue: `HoldReleasedEvent` was initialized twice due to a duplicated constructor block.
Fix: Removed the duplicate block and kept a single valid initialization.

## Warnings

[WARNING] File: docker-compose.yml, Line: ~43
Issue: The `api` service exposes port `5000:8080`, which is fine for local dev, but the API container is also referenced from `frontend` via Docker internal DNS.
Fix: No fix required for local dev, just verify network connectivity.

[WARNING] File: docker-compose.yml, Line: ~31
Issue: MongoDB environment variables must use `MongoDB__` rather than `MONGO_DB__` to bind to the `MongoDB` configuration section.
Fix: Updated environment variable names to `MongoDB__ConnectionString` and `MongoDB__DatabaseName`.

## Passed Checks

- `NuGet.Config` exists and contains `<clear />`.
- `InventoryHold.sln` exists with all 5 expected project references.
- `src/InventoryHold.Contracts/Enums/HoldStatus.cs` exists.
- `HoldService` uses `CacheKeys` for cache keys and invalidates inventory cache on state changes.
- `ReleaseHoldAsync` publishes a `HoldReleasedEvent` with persistent RabbitMQ properties.
- Frontend files exist and use relative `/api` endpoints.
- Dockerfiles exist for both API and frontend.
- `docs/architecture.md` exists and explains `availableStock` denormalization.

## Note

- Runtime validation through `dotnet build` and `dotnet test` could not be executed in the current environment because the `dotnet` SDK is unavailable.
