namespace SecureAuth.Models;

public sealed record StoredToken(string Value, TokenKind Kind, long ExpiresAtUnixTimeMilliseconds);
