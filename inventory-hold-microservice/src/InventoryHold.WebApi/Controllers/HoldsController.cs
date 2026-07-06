using InventoryHold.Contracts.Requests;
using InventoryHold.Contracts.Responses;
using InventoryHold.Domain.Services;
using InventoryHold.WebApi.Mappers;
using Microsoft.AspNetCore.Mvc;

namespace InventoryHold.WebApi.Controllers;

[ApiController]
[Route("api/holds")]
public sealed class HoldsController : ControllerBase
{
    private readonly HoldService _holdService;

    public HoldsController(HoldService holdService)
    {
        _holdService = holdService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateHold([FromBody] CreateHoldRequest request, CancellationToken ct)
    {
        var hold = await _holdService.CreateHoldAsync(request.ProductId, request.CustomerId, request.Quantity, request.TtlSeconds, ct);
        var response = HoldMapper.ToResponse(hold);
        return CreatedAtAction(nameof(GetHold), new { holdId = hold.Id }, response);
    }

    [HttpGet("{holdId}")]
    public async Task<IActionResult> GetHold(string holdId, CancellationToken ct)
    {
        var hold = await _holdService.GetHoldAsync(holdId, ct);
        return Ok(HoldMapper.ToResponse(hold));
    }

    [HttpDelete("{holdId}")]
    public async Task<IActionResult> ReleaseHold(string holdId, CancellationToken ct)
    {
        var hold = await _holdService.ReleaseHoldAsync(holdId, ct);
        return Ok(HoldMapper.ToResponse(hold));
    }
}
