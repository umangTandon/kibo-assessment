# Agent: Unit Tests
 
## Identity

 You are the **Unit Tests Agent** for the Inventory Hold Microservice. You write the complete xUnit test suite for `HoldService` business logic. Every test must be fully isolated — no running infrastructure required.
 
## Context

 Read `.github/copilot-instructions.md` for full project overview.

 Read `src/InventoryHold.Domain/Services/HoldService.cs` — this is the class under test.

 Read `src/InventoryHold.Domain/Entities/Hold.cs` — you need to understand the entity's state machine.

 Read `src/InventoryHold.Domain/Repositories/` and `src/InventoryHold.Domain/Ports/` — these are the interfaces you will mock.
 
## Scope — Files You Own

 ```

 src\InventoryHold.UnitTests\

 ```

 **Do not touch Contracts, Domain, Infrastructure, or WebApi.**
 
## Build Instructions
 
Delete `UnitTest1.cs`. Create:
 
---
 
### `Fixtures/HoldFixtures.cs`
 
Test data builders. `Hold` uses a private constructor and factory method — use `Hold.Create(...)` as the entry point. For the expired-but-still-Active state, set `ExpiresAt` into the past using reflection (the `InternalsVisibleTo` attribute on the Domain assembly allows this):
 
```csharp

 public static class HoldFixtures {

     public static Hold ActiveHold(

         string productId = "prod-001",

         string customerId = "cust-001",

         int quantity = 5,

         int ttlSeconds = 900)

         => Hold.Create(productId, customerId, quantity, ttlSeconds);
 
    // Simulates a hold that expired but was never cleaned up (status still Active)

     public static Hold ExpiredButActiveStatusHold() {

         var hold = Hold.Create("prod-001", "cust-001", 5, 1);

         typeof(Hold)

             .GetProperty(nameof(Hold.ExpiresAt))!

             .SetValue(hold, DateTime.UtcNow.AddMinutes(-10));

         return hold;

     }
 
    public static Hold ReleasedHold() {

         var hold = Hold.Create("prod-001", "cust-001", 5, 900);

         hold.Release();

         return hold;

     }
 
    public static Hold ExpiredHold() {

         var hold = Hold.Create("prod-001", "cust-001", 5, 900);

         hold.MarkExpired();

         return hold;

     }

 }

 ```
 
---
 
### `Services/HoldServiceTests.cs`
 
```csharp

 public class HoldServiceTests {

     private readonly Mock<IHoldRepository> _holdRepo = new();

     private readonly Mock<IInventoryRepository> _inventoryRepo = new();

     private readonly Mock<ICacheService> _cache = new();

     private readonly Mock<IMessagePublisher> _publisher = new();

     private readonly HoldService _sut;
 
    public HoldServiceTests() {

         // Default: cache always misses (returns null)

         _cache.Setup(c => c.GetAsync<Hold>(It.IsAny<string>(), It.IsAny<CancellationToken>()))

               .ReturnsAsync((Hold?)null);

         _cache.Setup(c => c.GetAsync<List<InventoryItem>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))

               .ReturnsAsync((List<InventoryItem>?)null);
 
        var options = Options.Create(new HoldOptions { DefaultTtlSeconds = 900 });

         _sut = new HoldService(

             _holdRepo.Object, _inventoryRepo.Object,

             _cache.Object, _publisher.Object,

             NullLogger<HoldService>.Instance, options);

     }

 ```
 
Implement the following tests:
 
---
 
**Test 1 — CreateHold when stock is sufficient returns active hold and publishes event**

 ```

 Arrange:

   - TryDeductStockAsync("prod-001", 5, default) → returns InventoryItem { ProductId="prod-001", TotalStock=100, ReservedStock=5 }

   - CreateAsync returns Task.CompletedTask

 Act:

   var hold = await _sut.CreateHoldAsync("prod-001", "cust-001", 5, null, default)

 Assert:

   - hold.Status == HoldStatus.Active

   - hold.ProductId == "prod-001"

   - hold.Quantity == 5

   - _holdRepo.Verify CreateAsync called once

   - _publisher.Verify PublishAsync("hold.created", It.IsAny<HoldCreatedEvent>(), default) called once

   - _cache.Verify RemoveAsync("inventory:all", default) called once

 ```
 
---
 
**Test 2 — CreateHold when stock insufficient throws InsufficientStockException**

 ```

 Arrange:

   - TryDeductStockAsync("prod-001", 999, default) → returns null

 Act:

   await _sut.CreateHoldAsync("prod-001", "cust-001", 999, null, default)

 Assert:

   - Throws InsufficientStockException

   - _holdRepo.Verify CreateAsync never called

   - _publisher.Verify PublishAsync never called (any args)

 ```
 
---
 
**Test 3 — ReleaseHold when active restores stock and publishes HoldReleasedEvent**

 ```

 Arrange:

   - activeHold = HoldFixtures.ActiveHold()

   - _holdRepo.GetByIdAsync(activeHold.Id, default) → returns activeHold

   - _inventoryRepo.RestoreStockAsync("prod-001", 5, default) → returns InventoryItem { AvailableStock=55 }

   - _holdRepo.UpdateAsync any → Task.CompletedTask

 Act:

   var result = await _sut.ReleaseHoldAsync(activeHold.Id, default)

 Assert:

   - result.Status == HoldStatus.Released

   - _inventoryRepo.Verify RestoreStockAsync called once

   - _publisher.Verify PublishAsync("hold.released", It.IsAny<HoldReleasedEvent>(), default) called once

 ```
 
---
 
**Test 4 — ReleaseHold when already released throws HoldAlreadyReleasedException**

 ```

 Arrange:

   - releasedHold = HoldFixtures.ReleasedHold()

   - _holdRepo.GetByIdAsync(releasedHold.Id, default) → returns releasedHold

 Act:

   await _sut.ReleaseHoldAsync(releasedHold.Id, default)

 Assert:

   - Throws HoldAlreadyReleasedException

   - _inventoryRepo.Verify RestoreStockAsync never called

   - _publisher.Verify PublishAsync never called

 ```
 
---
 
**Test 5 — GetHold when hold is expired but status still Active triggers lazy expiry**

 ```

 Arrange:

   - expiredHold = HoldFixtures.ExpiredButActiveStatusHold()

   - _holdRepo.GetByIdAsync(expiredHold.Id, default) → returns expiredHold

   - _inventoryRepo.RestoreStockAsync("prod-001", 5, default) → returns InventoryItem()

   - _holdRepo.UpdateAsync any → Task.CompletedTask

 Act:

   var result = await _sut.GetHoldAsync(expiredHold.Id, default)

 Assert:

   - result.Status == HoldStatus.Expired

   - _holdRepo.Verify UpdateAsync(It.Is<Hold>(h => h.Status == HoldStatus.Expired), default) called once

   - _inventoryRepo.Verify RestoreStockAsync called once

   - _publisher.Verify PublishAsync("hold.expired", It.IsAny<HoldExpiredEvent>(), default) called once

 ```
 
---
 
**Test 6 — GetHold when cache hit does not query database**

 ```

 Arrange:

   - cachedHold = HoldFixtures.ActiveHold()

   - _cache.GetAsync<Hold>($"hold:{cachedHold.Id}", default) → returns cachedHold (override default null setup)

 Act:

   var result = await _sut.GetHoldAsync(cachedHold.Id, default)

 Assert:

   - result.Id == cachedHold.Id

   - _holdRepo.Verify GetByIdAsync never called (for any args)

 ```
 
---
 
**Test 7 — CreateHold with zero quantity throws ArgumentException**

 ```

 Arrange: nothing special

 Act:

   await _sut.CreateHoldAsync("prod-001", null, 0, null, default)

 Assert:

   - Throws ArgumentException

   - _inventoryRepo.Verify TryDeductStockAsync never called

 ```
 
---
 
### `Domain/HoldEntityTests.cs`
 
5 tests for `Hold` entity state transitions — no mocks needed:
 
1. `Release_WhenActive_SetsStatusToReleased` — create active hold, call Release(), assert Status == Released

 2. `Release_WhenAlreadyReleased_ThrowsInvalidOperationException` — release twice, second throws

 3. `Release_WhenExpired_ThrowsInvalidOperationException` — MarkExpired first, then Release() throws

 4. `IsExpired_WhenExpiresAtInFuture_ReturnsFalse` — fresh hold, IsExpired() is false

 5. `MarkExpired_SetsStatusToExpired` — call MarkExpired(), Status == Expired
 
---
 
## Self-Review Checklist

 After implementing, verify:
 
- [ ] `dotnet test src/InventoryHold.UnitTests` exits with code 0 — all tests pass

 - [ ] Minimum 7 tests exist (5 in HoldServiceTests + some entity tests)

 - [ ] No test creates a MongoDB connection, Redis connection, or RabbitMQ connection

 - [ ] All `IHoldRepository`, `IInventoryRepository`, `ICacheService`, `IMessagePublisher` dependencies are mocked with Moq

 - [ ] `NullLogger<HoldService>` is used (from `Microsoft.Extensions.Logging.Abstractions`)

 - [ ] `Options.Create(new HoldOptions {...})` is used (from `Microsoft.Extensions.Options`)

 - [ ] FluentAssertions `Should().ThrowAsync<T>()` is used for exception assertions (not xUnit `Assert.ThrowsAsync`)

 - [ ] Test 5 (lazy expiry) verifies that `UpdateAsync` is called with `Status == Expired`

 - [ ] Test 6 (cache hit) verifies that `GetByIdAsync` is **never** called
 
## Constraints

 - Only write files in `src\InventoryHold.UnitTests\`

 - Tests must not spin up any HTTP server, database, or message broker

 - Use `FluentAssertions` for all assertions — do not mix with xUnit's `Assert.*`

 - Do not add integration test infrastructure — pure unit tests only