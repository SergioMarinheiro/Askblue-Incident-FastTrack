using ApiIncidentFastTrack.DTOs.ResolutionTypes;

namespace ApiIncidentFastTrack.Interfaces;

public interface IResolutionTypeService
{
    Task<IEnumerable<ResolutionTypeResponseDto>> GetResolutionTypesAsync(CancellationToken cancellationToken);
    Task<ResolutionTypeResponseDto> CreateResolutionTypeAsync(CreateResolutionTypeRequestDto request, CancellationToken cancellationToken);
    Task<ResolutionTypeResponseDto> UpdateResolutionTypeAsync(int id, UpdateResolutionTypeRequestDto request, CancellationToken cancellationToken);
}
