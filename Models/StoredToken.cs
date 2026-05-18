namespace SecureAuth.Models;

public sealed record StoredToken(string Value, TokenKind Kind, DateTimeOffset ExpiresAt);
