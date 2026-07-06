using FluentAssertions;
using InventoryHold.Domain.Entities;
using InventoryHold.UnitTests.Fixtures;

namespace InventoryHold.UnitTests.Domain;

public sealed class HoldEntityTests
{
    [Fact]
    public void Release_WhenActive_SetsStatusToReleased()
    {
        var hold = HoldFixtures.ActiveHold();

        hold.Release();

        hold.Status.Should().Be(InventoryHold.Contracts.Enums.HoldStatus.Released);
        hold.ReleasedAt.Should().NotBeNull();
    }

    [Fact]
    public void Release_WhenAlreadyReleased_ThrowsInvalidOperationException()
    {
        var hold = HoldFixtures.ReleasedHold();

        var act = () => hold.Release();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Release_WhenExpired_ThrowsInvalidOperationException()
    {
        var hold = HoldFixtures.ExpiredHold();

        var act = () => hold.Release();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtInFuture_ReturnsFalse()
    {
        var hold = HoldFixtures.ActiveHold();

        hold.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void MarkExpired_SetsStatusToExpired()
    {
        var hold = HoldFixtures.ActiveHold();

        hold.MarkExpired();

        hold.Status.Should().Be(InventoryHold.Contracts.Enums.HoldStatus.Expired);
        hold.ReleasedAt.Should().NotBeNull();
    }
}
