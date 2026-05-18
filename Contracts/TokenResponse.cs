namespace SecureAuth.Contracts;

public sealed record TokenResponse(string Token, DateTimeOffset ExpiresAt);
