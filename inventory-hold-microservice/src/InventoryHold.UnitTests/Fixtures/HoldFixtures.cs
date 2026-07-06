using InventoryHold.Domain.Entities;

namespace InventoryHold.UnitTests.Fixtures;

public static class HoldFixtures
{
    public static Hold ActiveHold(
        string productId = "prod-001",
        string customerId = "cust-001",
        int quantity = 5,
        int ttlSeconds = 900)
        => Hold.Create(productId, customerId, quantity, ttlSeconds);

    public static Hold ExpiredButActiveStatusHold()
    {
        var hold = Hold.Create("prod-001", "cust-001", 5, 1);
        typeof(Hold)
            .GetProperty(nameof(Hold.ExpiresAt))!
            .SetValue(hold, DateTime.UtcNow.AddMinutes(-10));
        return hold;
    }

    public static Hold ReleasedHold()
    {
        var hold = ActiveHold();
        hold.Release();
        return hold;
    }

    public static Hold ExpiredHold()
    {
        var hold = ActiveHold();
        hold.MarkExpired();
        return hold;
    }
}
