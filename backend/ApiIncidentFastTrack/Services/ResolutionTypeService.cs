using ApiIncidentFastTrack.DTOs.ResolutionTypes;
using ApiIncidentFastTrack.Interfaces;

namespace ApiIncidentFastTrack.Services;

// Contract only: implemented once the database, auth and tenant plumbing exist.
public class ResolutionTypeService : IResolutionTypeService
{
    public Task<IEnumerable<ResolutionTypeResponseDto>> GetResolutionTypesAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<ResolutionTypeResponseDto> CreateResolutionTypeAsync(CreateResolutionTypeRequestDto request, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<ResolutionTypeResponseDto> UpdateResolutionTypeAsync(int id, UpdateResolutionTypeRequestDto request, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
