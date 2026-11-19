using ApiIncidentFastTrack.DTOs.Stores;
using ApiIncidentFastTrack.Interfaces;

namespace ApiIncidentFastTrack.Services;

// Contract only: implemented once the database, auth and tenant plumbing exist.
public class StoreService : IStoreService
{
    public Task<IEnumerable<StoreResponseDto>> GetStoresAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<StoreResponseDto> GetStoreByIdAsync(int storeId, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<StoreResponseDto> CreateStoreAsync(CreateStoreRequestDto request, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<IEnumerable<LocationResponseDto>> GetLocationsAsync(int storeId, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<LocationResponseDto> CreateLocationAsync(int storeId, CreateLocationRequestDto request, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
