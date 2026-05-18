namespace SecureAuth.Models;

public sealed record UserRecord(string Login, string PasswordHash);
