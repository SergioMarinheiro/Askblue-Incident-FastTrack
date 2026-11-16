using ApiIncidentFastTrack.DTOs.Incidents;
using ApiIncidentFastTrack.Interfaces;

namespace ApiIncidentFastTrack.Services;

public class IncidentService : IIncidentService
{
    public Task<IEnumerable<IncidentResponseDto>> GetAllIncidentsAsync()
    {
        throw new NotImplementedException();
    }

    public Task<IncidentResponseDto?> GetIncidentByIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<IncidentResponseDto> CreateIncidentAsync(CreateIncidentRequestDto request)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteIncidentAsync(int id)
    {
        throw new NotImplementedException();
    }
}