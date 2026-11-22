using ApiIncidentFastTrack.Data;
using ApiIncidentFastTrack.DTOs.Stores;
using ApiIncidentFastTrack.Exceptions;
using ApiIncidentFastTrack.Interfaces;
using ApiIncidentFastTrack.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiIncidentFastTrack.Services;

// Tenant isolation:
// - Store has the "Tenant" query filter, so every store query only sees the caller's
//   company (SuperAdmin: all). Another company's store is simply "not found" (404).
// - Location has no filter. Every location operation loads its store first, through the filter.
// Soft delete has no global filter (by design), so deleted rows are excluded explicitly here.
public class StoreService : IStoreService
{
    private readonly AppDbContext _db;
    private readonly ICompanyResolver _companyResolver;

    public StoreService(AppDbContext db, ICompanyResolver companyResolver)
    {
        _db = db;
        _companyResolver = companyResolver;
    }

    public async Task<IEnumerable<StoreResponseDto>> GetStoresAsync(CancellationToken cancellationToken)
    {
        return await _db.Stores
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.Name)
            .Select(s => new StoreResponseDto
            {
                StoreId = s.Id,
                CompanyId = s.CompanyId,
                Name = s.Name,
                Address = s.Address
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<StoreResponseDto> GetStoreByIdAsync(int storeId, CancellationToken cancellationToken)
    {
        var store = await FindStoreAsync(storeId, tracking: false, cancellationToken);

        return ToDto(store);
    }

    public async Task<StoreResponseDto> CreateStoreAsync(CreateStoreRequestDto request, CancellationToken cancellationToken)
    {
        var companyId = await _companyResolver.ResolveForCreateAsync(request.CompanyId, "a store", cancellationToken);

        var store = new Store
        {
            CompanyId = companyId,
            Name = request.Name.Trim(),
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim()
        };

        _db.Stores.Add(store);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(store);
    }

    public async Task DeleteStoreAsync(int storeId, CancellationToken cancellationToken)
    {
        var store = await FindStoreAsync(storeId, tracking: true, cancellationToken);

        // AppDbContext.SaveChangesAsync turns this into a soft delete (IsDeleted, DeletedAt).
        // Its locations are left as they are: they are only reachable through the store,
        // so they disappear with it.
        _db.Stores.Remove(store);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<LocationResponseDto>> GetLocationsAsync(int storeId, CancellationToken cancellationToken)
    {
        await EnsureStoreExistsAsync(storeId, cancellationToken);

        return await _db.Locations
            .AsNoTracking()
            .Where(l => l.StoreId == storeId && !l.IsDeleted)
            .OrderBy(l => l.Name)
            .Select(l => new LocationResponseDto
            {
                LocationId = l.Id,
                StoreId = l.StoreId,
                Name = l.Name
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<LocationResponseDto> CreateLocationAsync(int storeId, CreateLocationRequestDto request, CancellationToken cancellationToken)
    {
        await EnsureStoreExistsAsync(storeId, cancellationToken);

        var location = new Location
        {
            StoreId = storeId,
            Name = request.Name.Trim()
        };

        _db.Locations.Add(location);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(location);
    }

    public async Task DeleteLocationAsync(int storeId, int locationId, CancellationToken cancellationToken)
    {
        await EnsureStoreExistsAsync(storeId, cancellationToken);

        // Matching on StoreId too: a location id from another store (possibly another
        // company's) must not be deletable through this store's URL.
        var location = await _db.Locations
            .SingleOrDefaultAsync(l => l.Id == locationId && l.StoreId == storeId && !l.IsDeleted, cancellationToken)
            ?? throw new NotFoundException($"Location {locationId} was not found in store {storeId}.");

        _db.Locations.Remove(location);
        await _db.SaveChangesAsync(cancellationToken);
    }

    // The store, through the tenant filter, not deleted. 404 otherwise.
    private async Task<Store> FindStoreAsync(int storeId, bool tracking, CancellationToken cancellationToken)
    {
        var query = tracking ? _db.Stores : _db.Stores.AsNoTracking();

        return await query.SingleOrDefaultAsync(s => s.Id == storeId && !s.IsDeleted, cancellationToken)
            ?? throw new NotFoundException($"Store {storeId} was not found.");
    }

    private async Task EnsureStoreExistsAsync(int storeId, CancellationToken cancellationToken)
    {
        var exists = await _db.Stores.AnyAsync(s => s.Id == storeId && !s.IsDeleted, cancellationToken);

        if (!exists)
        {
            throw new NotFoundException($"Store {storeId} was not found.");
        }
    }

    private static StoreResponseDto ToDto(Store store) => new()
    {
        StoreId = store.Id,
        CompanyId = store.CompanyId,
        Name = store.Name,
        Address = store.Address
    };

    private static LocationResponseDto ToDto(Location location) => new()
    {
        LocationId = location.Id,
        StoreId = location.StoreId,
        Name = location.Name
    };
}
