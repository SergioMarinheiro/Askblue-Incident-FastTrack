using ApiIncidentFastTrack.DTOs.Stores;
using ApiIncidentFastTrack.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

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

        if(stores.IsNullOrEmpty())
        {
            return NotFound();
        }

        return Ok(stores);
    }

    [HttpGet("{storeId:int}")]
    public async Task<IActionResult> GetStoreById(int storeId, CancellationToken cancellationToken)
    {
        var store = await _storeService.GetStoreByIdAsync(storeId, cancellationToken);

        if(store == null)
        {
            return NotFound();
        }

        return Ok(store);
    }

    [HttpPost]
    public async Task<IActionResult> CreateStore([FromBody] CreateStoreRequestDto request, CancellationToken cancellationToken)
    {
        var store = await _storeService.CreateStoreAsync(request, cancellationToken);

        if (store is null)
        {
            return NotFound("The specified company does not exist.");
        }

        return CreatedAtAction(
            nameof(GetStoreById),
            new { storeId = store.StoreId },
            store
        );
    }

    [HttpDelete("{storeId:int}")]
    public async Task<IActionResult> DeleteStore(int storeId, CancellationToken cancellationToken)
    {
        var deleted = await _storeService.DeleteStoreAsync(storeId, cancellationToken);

        if(!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpGet("{storeId:int}/locations")]
    public async Task<IActionResult> GetLocations(int storeId, CancellationToken cancellationToken)
    {
        var locations = await _storeService.GetLocationsAsync(storeId, cancellationToken);

        if(locations.IsNullOrEmpty())
        {
            return NotFound();
        }

        return Ok(locations);
    }

    [HttpPost("{storeId:int}/locations")]
    public async Task<IActionResult> CreateLocation(int storeId, [FromBody] CreateLocationRequestDto request, CancellationToken cancellationToken)
    {
        var location = await _storeService.CreateLocationAsync(storeId, request, cancellationToken);

        if (location is null)
        {
            return NotFound();
        }

        return CreatedAtAction(
            nameof(GetLocations),
            new { storeId },
            location
        );
    }

    [HttpDelete("{storeId:int}/locations/{locationId:int}")]
    public async Task<IActionResult> DeleteLocation(int storeId, int locationId, CancellationToken cancellationToken)
    {
        var deleted = await _storeService.DeleteLocationAsync(storeId, locationId, cancellationToken);

        if(!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
