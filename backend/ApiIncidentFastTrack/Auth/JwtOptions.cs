namespace ApiIncidentFastTrack.Auth;

// Bound from the "Jwt" configuration section.
// Issuer, Audience and ExpiryMinutes live in appsettings.json.
// SigningKey is a secret: user secrets locally, an environment variable or vault elsewhere.
public class JwtOptions
{
    public const string SectionName = "Jwt";

    // HMAC-SHA256 needs a key of at least 256 bits.
    public const int MinimumSigningKeyBytes = 32;

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}
