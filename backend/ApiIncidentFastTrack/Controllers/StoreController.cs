using ApiIncidentFastTrack.DTOs.Stores;
using ApiIncidentFastTrack.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ApiIncidentFastTrack.Controllers;

// Read: anyone in the company (to pick a store/location on an incident).
// Create: Admin (own company) and SuperAdmin.
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
    public async Task<IActionResult> CreateStore([FromBody] CreateStoreRequestDto request, CancellationToken cancellationToken)
    {
        var store = await _storeService.CreateStoreAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetStoreById),
            new { storeId = store.StoreId },
            store
        );
    }

    [HttpGet("{storeId:int}/locations")]
    public async Task<IActionResult> GetLocations(int storeId, CancellationToken cancellationToken)
    {
        var locations = await _storeService.GetLocationsAsync(storeId, cancellationToken);

        return Ok(locations);
    }

    [HttpPost("{storeId:int}/locations")]
    public async Task<IActionResult> CreateLocation(int storeId, [FromBody] CreateLocationRequestDto request, CancellationToken cancellationToken)
    {
        var location = await _storeService.CreateLocationAsync(storeId, request, cancellationToken);

        return CreatedAtAction(
            nameof(GetLocations),
            new { storeId },
            location
        );
    }
}
