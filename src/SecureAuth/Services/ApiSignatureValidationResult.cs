namespace SecureAuth.Services;

public enum ApiSignatureValidationResult
{
    Success,
    InvalidSignature,
    StaleRequest
}
