using ApiIncidentFastTrack.DTOs.Incidents;

namespace ApiIncidentFastTrack.Interfaces;
public interface IIncidentService
{
    Task<IEnumerable<IncidentResponseDto>> GetAllIncidentsAsync();
    Task<IncidentResponseDto?> GetIncidentByIdAsync(int id);
    Task<IncidentResponseDto> CreateIncidentAsync(CreateIncidentRequestDto request);
}