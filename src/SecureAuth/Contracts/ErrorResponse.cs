using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace SecureAuth.Contracts;

public sealed record ErrorResponse(
    [property: JsonPropertyName("error")] string Error,
    [property: JsonPropertyName("message")] string Message)
{
    public static ErrorResponse InvalidRequest() => new("invalid_request", "Invalid request body.");

    public static ErrorResponse InvalidSignature() => new("invalid_signature", "Request signature is invalid.");

    public static ErrorResponse StaleRequest() => new("stale_request", "Request date is outside the allowed freshness window.");

    public static ErrorResponse InvalidCredentials() => new("invalid_credentials", "Invalid login or password.");

    public static ErrorResponse InvalidSimpleToken() => new("invalid_simple_token", "Simple token is invalid or expired.");

    public static ErrorResponse InvalidFullToken() => new("invalid_full_token", "Full token is invalid or expired.");

    public static ErrorResponse InternalServerError() => new("internal_server_error", "Unexpected server error.");

    public static ErrorResponse FromStatusCode(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status404NotFound => new ErrorResponse("not_found", "Endpoint was not found."),
            StatusCodes.Status405MethodNotAllowed => new ErrorResponse("method_not_allowed", "HTTP method is not allowed for this endpoint."),
            _ => new ErrorResponse("http_error", "Request failed.")
        };
    }
}
