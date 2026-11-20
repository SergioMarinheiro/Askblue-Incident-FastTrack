using ApiIncidentFastTrack.Data;
using ApiIncidentFastTrack.DTOs.Stores;
using ApiIncidentFastTrack.Interfaces;
using ApiIncidentFastTrack.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;

namespace ApiIncidentFastTrack.Services;

// Contract only: implemented once the database, auth and tenant plumbing exist.
public class StoreService : IStoreService
{
    private readonly AppDbContext _context;

    public StoreService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<StoreResponseDto>> GetStoresAsync(CancellationToken cancellationToken)
    {
        return await _context.Stores
            .Where(s => !s.IsDeleted)
            .Select(s => new StoreResponseDto
            {
                StoreId = s.Id,
                CompanyId = s.CompanyId,
                Name = s.Name,
                Address = s.Address
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<StoreResponseDto?> GetStoreByIdAsync(int storeId, CancellationToken cancellationToken)
    {
        return await _context.Stores
            .Where(s => !s.IsDeleted && s.Id == storeId)
            .Select(s => new StoreResponseDto
            {
                StoreId = s.Id,
                CompanyId = s.CompanyId,
                Name = s.Name,
                Address = s.Address
            }).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<StoreResponseDto> CreateStoreAsync(CreateStoreRequestDto request, CancellationToken cancellationToken)
    {
        var store = new Store
        {
            Name = request.Name,
            Address = request.Address ?? null
        };

        await _context.Stores.AddAsync(store, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new StoreResponseDto
        {
            StoreId = store.Id,
            CompanyId = store.CompanyId,
            Name = store.Name,
            Address = store.Address
        };
    }

    public async Task<bool> DeleteStoreAsync(int storeId, CancellationToken cancellationToken)
    {
        var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == storeId && !s.IsDeleted, cancellationToken);

        if (store is null)
        {
            return false;
        }

        _context.Stores.Remove(store);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IEnumerable<LocationResponseDto>> GetLocationsAsync(int storeId, CancellationToken cancellationToken)
    {
        return await _context.Locations
            .Where(location =>
                location.StoreId == storeId &&
                !location.IsDeleted)
            .Select(location => new LocationResponseDto
            {
                LocationId = location.Id,
                StoreId = location.StoreId,
                Name = location.Name
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<LocationResponseDto?> CreateLocationAsync(int storeId, CreateLocationRequestDto request,
        CancellationToken cancellationToken)
    {
        var storeExists = await _context.Stores.AnyAsync(store => store.Id == storeId && !store.IsDeleted, cancellationToken);

        if (!storeExists)
        {
            return null;
        }

        var location = new Location
        {
            StoreId = storeId,
            Name = request.Name
        };

        await _context.Locations.AddAsync(location, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new LocationResponseDto
        {
            LocationId = location.Id,
            StoreId = location.StoreId,
            Name = location.Name
        };
    }

    public async Task<bool> DeleteLocationAsync(int storeId, int locationId, CancellationToken cancellationToken)
    {
        var location = await _context.Locations.FirstOrDefaultAsync(l =>
                l.Id == locationId &&
                l.StoreId == storeId &&
                !l.IsDeleted &&
                !l.Store.IsDeleted,
            cancellationToken);

        if (location is null)
        {
            return false;
        }

        _context.Locations.Remove(location);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}