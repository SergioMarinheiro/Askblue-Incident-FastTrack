using ApiIncidentFastTrack.Models;

namespace ApiIncidentFastTrack.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(User user);
}
