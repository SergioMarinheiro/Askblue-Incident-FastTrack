using ApiIncidentFastTrack.DTOs.Stores;

namespace ApiIncidentFastTrack.Interfaces;

// Stores and the locations inside them.
public interface IStoreService
{
    Task<IEnumerable<StoreResponseDto>> GetStoresAsync(CancellationToken cancellationToken);
    Task<StoreResponseDto> GetStoreByIdAsync(int storeId, CancellationToken cancellationToken);
    Task<StoreResponseDto> CreateStoreAsync(CreateStoreRequestDto request, CancellationToken cancellationToken);

    Task<IEnumerable<LocationResponseDto>> GetLocationsAsync(int storeId, CancellationToken cancellationToken);
    Task<LocationResponseDto> CreateLocationAsync(int storeId, CreateLocationRequestDto request, CancellationToken cancellationToken);
}
