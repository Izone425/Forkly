namespace Forkly.Api.Services;

// Bound from the "Jwt" config section. The signing Key must come from a secret
// store (user-secrets in dev, env var / key vault in prod) — never commit it.
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "forkly-api";
    public string Audience { get; set; } = "forkly-clients";
    public string Key { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 120;
}
