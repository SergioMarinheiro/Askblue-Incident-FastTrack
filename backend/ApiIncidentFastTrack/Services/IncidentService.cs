using ApiIncidentFastTrack.DTOs.Incidents;
using ApiIncidentFastTrack.Interfaces;

namespace ApiIncidentFastTrack.Services;

// Contract only: implemented once the database, auth and tenant plumbing exist.
public class IncidentService : IIncidentService
{
    public Task<IEnumerable<IncidentResponseDto>> GetAllIncidentsAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<IncidentResponseDto> GetIncidentByIdAsync(int id, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<IncidentResponseDto> CreateIncidentAsync(CreateIncidentRequestDto request, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<IncidentResponseDto> UpdateIncidentAsync(int id, UpdateIncidentRequestDto request, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task DeleteDuplicateIncidentAsync(int id, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<SubmitIncidentResultDto> SubmitIncidentAsync(int id, SubmitIncidentRequestDto request, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<IncidentResponseDto> ApproveIncidentAsync(int id, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<IncidentResponseDto> RejectIncidentAsync(int id, RejectIncidentRequestDto request, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<IncidentResponseDto> SendBackIncidentAsync(int id, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<IncidentResponseDto> CompleteIncidentAsync(int id, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<IncidentResponseDto> CancelIncidentAsync(int id, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<IEnumerable<IncidentDuplicateResponseDto>> GetDuplicateFlagsAsync(int id, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<IncidentResponseDto> DismissDuplicateFlagsAsync(int id, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<IncidentResponseDto> ConfirmDuplicateAsync(int id, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
