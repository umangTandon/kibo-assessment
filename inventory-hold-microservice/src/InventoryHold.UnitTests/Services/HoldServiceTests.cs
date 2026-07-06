using FluentAssertions;
using InventoryHold.Contracts.Events;
using InventoryHold.Domain.Entities;
using InventoryHold.Domain.Exceptions;
using InventoryHold.Domain.Options;
using InventoryHold.Domain.Ports;
using InventoryHold.Domain.Repositories;
using InventoryHold.Domain.Services;
using InventoryHold.UnitTests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace InventoryHold.UnitTests.Services;

public sealed class HoldServiceTests
{
    private readonly Mock<IHoldRepository> _holdRepo = new();
    private readonly Mock<IInventoryRepository> _inventoryRepo = new();
    private readonly Mock<ICacheService> _cache = new();
    private readonly Mock<IMessagePublisher> _publisher = new();
    private readonly HoldService _sut;

    public HoldServiceTests()
    {
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

    [Fact]
    public async Task CreateHoldAsync_WhenStockIsAvailable_CreatesHoldAndPublishesEvent()
    {
        var inventory = new InventoryItem
        {
            ProductId = "prod-001",
            ProductName = "Widget Alpha",
            TotalStock = 100,
            ReservedStock = 5,
            Version = 1
        };

        _inventoryRepo.Setup(r => r.TryDeductStockAsync("prod-001", 5, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(inventory);
        _holdRepo.Setup(r => r.CreateAsync(It.IsAny<Hold>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _sut.CreateHoldAsync("prod-001", "cust-001", 5, null, default);

        result.Status.Should().Be(InventoryHold.Contracts.Enums.HoldStatus.Active);
        result.ProductId.Should().Be("prod-001");
        result.Quantity.Should().Be(5);
        _holdRepo.Verify(r => r.CreateAsync(It.IsAny<Hold>(), default), Times.Once);
        _publisher.Verify(p => p.PublishAsync("hold.created", It.IsAny<HoldCreatedEvent>(), default), Times.Once);
    }

    [Fact]
    public async Task CreateHoldAsync_WhenStockIsInsufficient_ThrowsInsufficientStockException()
    {
        _inventoryRepo.Setup(r => r.TryDeductStockAsync("prod-001", 999, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((InventoryItem?)null);

        var act = async () => await _sut.CreateHoldAsync("prod-001", null, 999, null, default);

        await act.Should().ThrowAsync<InsufficientStockException>();
        _holdRepo.Verify(r => r.CreateAsync(It.IsAny<Hold>(), It.IsAny<CancellationToken>()), Times.Never);
        _publisher.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReleaseHoldAsync_WhenHoldIsActive_RestoresStockAndPublishesEvent()
    {
        var activeHold = HoldFixtures.ActiveHold();
        var restoredInventory = new InventoryItem
        {
            ProductId = "prod-001",
            ProductName = "Widget Alpha",
            TotalStock = 100,
            ReservedStock = 0,
            Version = 2
        };

        _holdRepo.Setup(r => r.GetByIdAsync(activeHold.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(activeHold);
        _inventoryRepo.Setup(r => r.RestoreStockAsync("prod-001", 5, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(restoredInventory);
        _holdRepo.Setup(r => r.UpdateAsync(activeHold, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _sut.ReleaseHoldAsync(activeHold.Id, default);

        result.Status.Should().Be(InventoryHold.Contracts.Enums.HoldStatus.Released);
        _inventoryRepo.Verify(r => r.RestoreStockAsync("prod-001", 5, default), Times.Once);
        _publisher.Verify(p => p.PublishAsync("hold.released", It.IsAny<HoldReleasedEvent>(), default), Times.Once);
    }

    [Fact]
    public async Task ReleaseHoldAsync_WhenHoldAlreadyReleased_ThrowsHoldAlreadyReleasedException()
    {
        var releasedHold = HoldFixtures.ReleasedHold();
        _holdRepo.Setup(r => r.GetByIdAsync(releasedHold.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(releasedHold);

        var act = async () => await _sut.ReleaseHoldAsync(releasedHold.Id, default);

        await act.Should().ThrowAsync<HoldAlreadyReleasedException>();
        _inventoryRepo.Verify(r => r.RestoreStockAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _publisher.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetHoldAsync_WhenHoldIsExpired_TransitionsToExpiredAndPublishesEvent()
    {
        var expiredHold = HoldFixtures.ExpiredButActiveStatusHold();
        var inventory = new InventoryItem
        {
            ProductId = expiredHold.ProductId,
            ProductName = "Widget Alpha",
            TotalStock = 100,
            ReservedStock = 0,
            Version = 2
        };

        _holdRepo.Setup(r => r.GetByIdAsync(expiredHold.Id, It.IsAny<CancellationToken>())).ReturnsAsync(expiredHold);
        _inventoryRepo.Setup(r => r.RestoreStockAsync(expiredHold.ProductId, expiredHold.Quantity, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(inventory);
        _holdRepo.Setup(r => r.UpdateAsync(It.IsAny<Hold>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _sut.GetHoldAsync(expiredHold.Id, default);

        result.Status.Should().Be(InventoryHold.Contracts.Enums.HoldStatus.Expired);
        _holdRepo.Verify(r => r.UpdateAsync(It.Is<Hold>(h => h.Status == InventoryHold.Contracts.Enums.HoldStatus.Expired), default), Times.Once);
        _inventoryRepo.Verify(r => r.RestoreStockAsync(It.IsAny<string>(), It.IsAny<int>(), default), Times.Once);
        _publisher.Verify(p => p.PublishAsync("hold.expired", It.IsAny<HoldExpiredEvent>(), default), Times.Once);
    }

    [Fact]
    public async Task GetHoldAsync_WhenCacheHit_ReturnsCachedHoldWithoutDatabaseCall()
    {
        var cachedHold = HoldFixtures.ActiveHold();
        _cache.Setup(c => c.GetAsync<Hold>(It.Is<string>(k => k == $"hold:{cachedHold.Id}"), It.IsAny<CancellationToken>()))
              .ReturnsAsync(cachedHold);

        var result = await _sut.GetHoldAsync(cachedHold.Id, default);

        result.Id.Should().Be(cachedHold.Id);
        _holdRepo.Verify(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateHoldAsync_WhenQuantityIsZero_ThrowsArgumentException()
    {
        var act = async () => await _sut.CreateHoldAsync("prod-001", null, 0, null, default);

        await act.Should().ThrowAsync<ArgumentException>();
        _inventoryRepo.Verify(r => r.TryDeductStockAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
