using ApiIncidentFastTrack.DTOs.Incidents;

namespace ApiIncidentFastTrack.Interfaces;

// No method takes a companyId or userId: the caller's identity and company
// come from the authenticated user (JWT), never from the request.
// Failures are reported with exceptions (not found / forbidden / business rule),
// translated to ProblemDetails by a central handler.
public interface IIncidentService
{
    // Reading
    Task<IEnumerable<IncidentResponseDto>> GetAllIncidentsAsync(CancellationToken cancellationToken);
    Task<IncidentResponseDto> GetIncidentByIdAsync(int id, CancellationToken cancellationToken);
    Task<IEnumerable<IncidentHistoryResponseDto>> GetIncidentHistoryAsync(int id, CancellationToken cancellationToken);

    // Draft editing
    Task<IncidentResponseDto> CreateIncidentAsync(CreateIncidentRequestDto request, CancellationToken cancellationToken);
    Task<IncidentResponseDto> UpdateIncidentAsync(int id, UpdateIncidentRequestDto request, CancellationToken cancellationToken);

    // Hard delete: only when Staff acknowledge a duplicate found at submit
    Task DeleteDuplicateIncidentAsync(int id, CancellationToken cancellationToken);

    // Status transitions
    Task<SubmitIncidentResultDto> SubmitIncidentAsync(int id, SubmitIncidentRequestDto request, CancellationToken cancellationToken);
    Task<IncidentResponseDto> ApproveIncidentAsync(int id, CancellationToken cancellationToken);
    Task<IncidentResponseDto> RejectIncidentAsync(int id, RejectIncidentRequestDto request, CancellationToken cancellationToken);
    Task<IncidentResponseDto> SendBackIncidentAsync(int id, SendBackIncidentRequestDto request, CancellationToken cancellationToken);
    Task<IncidentResponseDto> CompleteIncidentAsync(int id, CancellationToken cancellationToken);
    Task<IncidentResponseDto> CancelIncidentAsync(int id, CancellationToken cancellationToken);

    // Duplicate flags (Manager review)
    Task<IEnumerable<IncidentDuplicateResponseDto>> GetDuplicateFlagsAsync(int id, CancellationToken cancellationToken);
    Task<IncidentResponseDto> DismissDuplicateFlagsAsync(int id, CancellationToken cancellationToken);
    Task<IncidentResponseDto> ConfirmDuplicateAsync(int id, ConfirmDuplicateRequestDto request, CancellationToken cancellationToken);
}
