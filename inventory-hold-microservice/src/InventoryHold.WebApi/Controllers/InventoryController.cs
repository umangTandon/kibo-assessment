using InventoryHold.Contracts.Responses;
using InventoryHold.Domain.Services;
using InventoryHold.WebApi.Mappers;
using Microsoft.AspNetCore.Mvc;

namespace InventoryHold.WebApi.Controllers;

[ApiController]
[Route("api/inventory")]
public sealed class InventoryController : ControllerBase
{
    private readonly HoldService _holdService;

    public InventoryController(HoldService holdService)
    {
        _holdService = holdService;
    }

    [HttpGet]
    public async Task<IActionResult> GetInventory(CancellationToken ct)
    {
        var inventory = await _holdService.GetInventoryAsync(ct);
        return Ok(inventory.Select(InventoryMapper.ToResponse));
    }
}
