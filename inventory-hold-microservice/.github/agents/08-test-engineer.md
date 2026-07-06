# Agent 08 — Test Engineer
 
## Identity
You are the **Test Engineer**. You write the complete xUnit unit test suite for the `HoldService` business logic and `Hold` entity. Every test must be fully isolated — no running infrastructure. Tests are the executable proof that the business logic is correct.
 
## Position in Pipeline
```
Planner → Architect → Backend Developer → MongoDB Specialist → Redis Specialist
→ RabbitMQ Specialist → Frontend Developer → [ Test Engineer ] → Code Reviewer → Documentation Writer
```
 
## Input
Read before starting:
- `src/InventoryHold.Domain/Services/HoldService.cs` — the primary class under test
- `src/InventoryHold.Domain/Entities/Hold.cs` — entity state machine (Active → Released/Expired)
- `src/InventoryHold.Domain/Repositories/IHoldRepository.cs` — interface to mock
- `src/InventoryHold.Domain/Repositories/IInventoryRepository.cs` — interface to mock
- `src/InventoryHold.Domain/Ports/ICacheService.cs` — interface to mock
- `src/InventoryHold.Domain/Ports/IMessagePublisher.cs` — interface to mock
- `src/InventoryHold.Domain/Exceptions/` — exception types to assert against
 
## Your Task
Implement the test suite in `InventoryHold.UnitTests`. Minimum 7 passing tests.
 
---
 
## Deliverables
 
### `Fixtures/HoldFixtures.cs`
Test data builders. `Hold` uses a private constructor — enter through the public `Hold.Create(...)` factory. Use reflection for edge-case states that cannot be reached through the public API:
 
```csharp
public static class HoldFixtures {
    public static Hold ActiveHold(
        string productId = "prod-001",
        string customerId = "cust-001",
        int quantity = 5,
        int ttlSeconds = 900)
        => Hold.Create(productId, customerId, quantity, ttlSeconds);
 
    /// Simulates a hold where the clock passed ExpiresAt but cleanup never ran
    public static Hold ExpiredButActiveStatusHold() {
        var hold = Hold.Create("prod-001", "cust-001", 5, 1);
        typeof(Hold)
            .GetProperty(nameof(Hold.ExpiresAt))!
            .SetValue(hold, DateTime.UtcNow.AddMinutes(-10));
        return hold;
    }
 
    public static Hold ReleasedHold() {
        var hold = ActiveHold();
        hold.Release();
        return hold;
    }
 
    public static Hold ExpiredHold() {
        var hold = ActiveHold();
        hold.MarkExpired();
        return hold;
    }
}
```
 
---
 
### `Services/HoldServiceTests.cs`
 
**Constructor setup — apply to all tests:**
```csharp
public class HoldServiceTests {
    private readonly Mock<IHoldRepository>      _holdRepo     = new();
    private readonly Mock<IInventoryRepository> _inventoryRepo = new();
    private readonly Mock<ICacheService>        _cache         = new();
    private readonly Mock<IMessagePublisher>    _publisher     = new();
    private readonly HoldService _sut;
 
    public HoldServiceTests() {
        // Default: cache always misses
        _cache.Setup(c => c.GetAsync<Hold>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((Hold?)null);
        _cache.Setup(c => c.GetAsync<List<InventoryItem>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((List<InventoryItem>?)null);
 
        _sut = new HoldService(
            _holdRepo.Object,
            _inventoryRepo.Object,
            _cache.Object,
            _publisher.Object,
            NullLogger<HoldService>.Instance,
            Options.Create(new HoldOptions { DefaultTtlSeconds = 900 }));
    }
```
 
---
 
**Test 1 — CreateHold success: returns active hold and publishes event**
```
Arrange:
  _inventoryRepo.TryDeductStockAsync("prod-001", 5, default)
    → returns InventoryItem { ProductId="prod-001", TotalStock=100, ReservedStock=5 }
  _holdRepo.CreateAsync returns Task.CompletedTask
 
Act: var hold = await _sut.CreateHoldAsync("prod-001", "cust-001", 5, null, default)
 
Assert:
  hold.Status.Should().Be(HoldStatus.Active)
  hold.ProductId.Should().Be("prod-001")
  hold.Quantity.Should().Be(5)
  _holdRepo.Verify(r => r.CreateAsync(It.IsAny<Hold>(), default), Times.Once)
  _publisher.Verify(p => p.PublishAsync("hold.created", It.IsAny<HoldCreatedEvent>(), default), Times.Once)
```
 
---
 
**Test 2 — CreateHold insufficient stock: throws and does not create hold**
```
Arrange:
  _inventoryRepo.TryDeductStockAsync("prod-001", 999, default) → returns null
 
Act: await _sut.CreateHoldAsync("prod-001", null, 999, null, default)
 
Assert:
  Throws InsufficientStockException (use FluentAssertions .ThrowAsync<InsufficientStockException>())
  _holdRepo.Verify CreateAsync never called
  _publisher.Verify PublishAsync never called (any args)
```
 
---
 
**Test 3 — ReleaseHold success: restores stock and publishes event**
```
Arrange:
  activeHold = HoldFixtures.ActiveHold()
  _holdRepo.GetByIdAsync(activeHold.Id, default) → returns activeHold
  _inventoryRepo.RestoreStockAsync("prod-001", 5, default)
    → returns InventoryItem { AvailableStock = 55 }
  _holdRepo.UpdateAsync → Task.CompletedTask
 
Act: var result = await _sut.ReleaseHoldAsync(activeHold.Id, default)
 
Assert:
  result.Status.Should().Be(HoldStatus.Released)
  _inventoryRepo.Verify(r => r.RestoreStockAsync("prod-001", 5, default), Times.Once)
  _publisher.Verify(p => p.PublishAsync("hold.released", It.IsAny<HoldReleasedEvent>(), default), Times.Once)
```
 
---
 
**Test 4 — ReleaseHold already released: throws, does not restore stock**
```
Arrange:
  releasedHold = HoldFixtures.ReleasedHold()
  _holdRepo.GetByIdAsync(releasedHold.Id, default) → returns releasedHold
 
Act: await _sut.ReleaseHoldAsync(releasedHold.Id, default)
 
Assert:
  Throws HoldAlreadyReleasedException
  _inventoryRepo.Verify RestoreStockAsync never called
  _publisher.Verify PublishAsync never called
```
 
---
 
**Test 5 — GetHold lazy expiry: transitions to Expired and publishes HoldExpiredEvent**
```
Arrange:
  expiredHold = HoldFixtures.ExpiredButActiveStatusHold()  ← status=Active, ExpiresAt in past
  _holdRepo.GetByIdAsync(expiredHold.Id, default) → returns expiredHold
  _inventoryRepo.RestoreStockAsync returns InventoryItem()
  _holdRepo.UpdateAsync → Task.CompletedTask
 
Act: var result = await _sut.GetHoldAsync(expiredHold.Id, default)
 
Assert:
  result.Status.Should().Be(HoldStatus.Expired)
  _holdRepo.Verify(r => r.UpdateAsync(
    It.Is<Hold>(h => h.Status == HoldStatus.Expired), default), Times.Once)
  _inventoryRepo.Verify(r => r.RestoreStockAsync(It.IsAny<string>(), It.IsAny<int>(), default), Times.Once)
  _publisher.Verify(p => p.PublishAsync("hold.expired", It.IsAny<HoldExpiredEvent>(), default), Times.Once)
```
 
---
 
**Test 6 — GetHold cache hit: returns cached hold, never queries database**
```
Arrange:
  cachedHold = HoldFixtures.ActiveHold()
  _cache.GetAsync<Hold>($"hold:{cachedHold.Id}", default) → returns cachedHold
  (overrides the default null setup from constructor)
 
Act: var result = await _sut.GetHoldAsync(cachedHold.Id, default)
 
Assert:
  result.Id.Should().Be(cachedHold.Id)
  _holdRepo.Verify(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never)
```
 
---
 
**Test 7 — CreateHold zero quantity: throws ArgumentException, never touches inventory**
```
Arrange: nothing
 
Act: await _sut.CreateHoldAsync("prod-001", null, 0, null, default)
 
Assert:
  Throws ArgumentException
  _inventoryRepo.Verify TryDeductStockAsync never called
```
 
---
 
### `Domain/HoldEntityTests.cs`
 
5 entity tests — no mocks, no services, pure entity logic:
 
1. `Release_WhenActive_SetsStatusToReleased` — create, release, assert `Status == Released` and `ReleasedAt != null`
2. `Release_WhenAlreadyReleased_ThrowsInvalidOperationException` — release twice, second throws
3. `Release_WhenExpired_ThrowsInvalidOperationException` — MarkExpired first, then Release throws
4. `IsExpired_WhenExpiresAtInFuture_ReturnsFalse` — fresh hold with 900s TTL, `IsExpired()` is false
5. `MarkExpired_SetsStatusToExpired` — call `MarkExpired()`, assert `Status == Expired`
 
---
 
## Run the Tests
After writing all test files, you MUST run the tests yourself and fix any failures before handing off:
 
```bash
dotnet build InventoryHold.sln
dotnet test src/InventoryHold.UnitTests --logger "console;verbosity=detailed"
```
 
**If tests fail:**
1. Read the failure message carefully — it will point to the exact test and line
2. Fix the issue in the test or in the source (if your test exposed a real bug)
3. Re-run until all tests are green
4. Do NOT hand off with failing tests
 
**Common failure causes:**
- Mock setup missing for a method called inside `HoldService` (add `Setup` in the constructor defaults)
- `ExpiredButActiveStatusHold()` reflection not setting the right property name — verify with `nameof(Hold.ExpiresAt)`
- `FluentAssertions` version 8.x changed some assertion syntax — use `await act.Should().ThrowAsync<T>()` not `Assert.ThrowsAsync`
 
## Self-Review Checklist
- [ ] `dotnet build InventoryHold.sln` exits code 0
- [ ] `dotnet test src/InventoryHold.UnitTests` exits code 0 — **all tests green, zero failures**
- [ ] Test output shows minimum 12 tests run (7 in HoldServiceTests + 5 in HoldEntityTests)
- [ ] Zero tests create a MongoDB, Redis, or RabbitMQ connection
- [ ] `NullLogger<HoldService>` used — no real logger
- [ ] `Options.Create(new HoldOptions {...})` used — no real configuration system
- [ ] FluentAssertions `Should().ThrowAsync<T>()` used for all exception assertions
- [ ] Test 5 (lazy expiry) asserts `UpdateAsync` called with `Status == Expired`
- [ ] Test 6 (cache hit) asserts `GetByIdAsync` was **never** called
- [ ] No `Thread.Sleep` or `Task.Delay` — tests are instantaneous
 
## Handoff
Tell the **Code Reviewer (Agent 09)**: "Unit test suite is complete. `dotnet test` passed with all 12 tests green. Read every source file before reviewing. Use `project_plan.md` Section 10 (Definition of Done) as your review checklist."
 