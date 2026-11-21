using ApiIncidentFastTrack.Auth;
using ApiIncidentFastTrack.DTOs.Stores;
using ApiIncidentFastTrack.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiIncidentFastTrack.Controllers;

// Read: anyone in the company (to pick a store/location on an incident).
// Create and delete (soft): Admin (own company) and SuperAdmin.
[ApiController]
[Route("api/stores")]
public class StoreController : ControllerBase
{
    private readonly IStoreService _storeService;

    public StoreController(IStoreService storeService)
    {
        _storeService = storeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetStores(CancellationToken cancellationToken)
    {
        var stores = await _storeService.GetStoresAsync(cancellationToken);

        return Ok(stores);
    }

    [HttpGet("{storeId:int}")]
    public async Task<IActionResult> GetStoreById(int storeId, CancellationToken cancellationToken)
    {
        var store = await _storeService.GetStoreByIdAsync(storeId, cancellationToken);

        return Ok(store);
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.AdminOrAbove)]
    public async Task<IActionResult> CreateStore([FromBody] CreateStoreRequestDto request, CancellationToken cancellationToken)
    {
        var store = await _storeService.CreateStoreAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetStoreById),
            new { storeId = store.StoreId },
            store
        );
    }

    [HttpDelete("{storeId:int}")]
    [Authorize(Policy = AuthPolicies.AdminOrAbove)]
    public async Task<IActionResult> DeleteStore(int storeId, CancellationToken cancellationToken)
    {
        await _storeService.DeleteStoreAsync(storeId, cancellationToken);

        return NoContent();
    }

    [HttpGet("{storeId:int}/locations")]
    public async Task<IActionResult> GetLocations(int storeId, CancellationToken cancellationToken)
    {
        var locations = await _storeService.GetLocationsAsync(storeId, cancellationToken);

        return Ok(locations);
    }

    [HttpPost("{storeId:int}/locations")]
    [Authorize(Policy = AuthPolicies.AdminOrAbove)]
    public async Task<IActionResult> CreateLocation(int storeId, [FromBody] CreateLocationRequestDto request, CancellationToken cancellationToken)
    {
        var location = await _storeService.CreateLocationAsync(storeId, request, cancellationToken);

        return CreatedAtAction(
            nameof(GetLocations),
            new { storeId },
            location
        );
    }

    [HttpDelete("{storeId:int}/locations/{locationId:int}")]
    [Authorize(Policy = AuthPolicies.AdminOrAbove)]
    public async Task<IActionResult> DeleteLocation(int storeId, int locationId, CancellationToken cancellationToken)
    {
        await _storeService.DeleteLocationAsync(storeId, locationId, cancellationToken);

        return NoContent();
    }
}
