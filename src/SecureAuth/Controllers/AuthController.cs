using Microsoft.AspNetCore.Mvc;
using SecureAuth.Contracts;
using SecureAuth.Filters;
using SecureAuth.Services;

namespace SecureAuth.Controllers;

[ApiController]
[Route("auth")]
[ValidateApiSignature]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Starts the auth flow: checks credentials and returns a short-lived simple token.
    /// </summary>
    /// <param name="request">Login, password and signature fields from the client.</param>
    /// <response code="200">The login was accepted and a simple token was issued.</response>
    /// <response code="400">The request body could not be read or is missing required data.</response>
    /// <response code="401">Credentials or request signature are invalid.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public ActionResult<TokenResponse> Login(LoginRequest request)
    {
        var token = _authService.Login(request.Login!, request.Password!);

        if (token is null)
        {
            return Unauthorized(ErrorResponse.InvalidCredentials());
        }

        return Ok(token);
    }

    /// <summary>
    /// Consumes a simple token and returns the full token used by the rest of the flow.
    /// </summary>
    /// <param name="request">Simple token and signature fields from the client.</param>
    /// <response code="200">The simple token was valid and a full token was issued.</response>
    /// <response code="400">The request body could not be read or is missing required data.</response>
    /// <response code="401">The simple token or request signature is invalid.</response>
    [HttpPost("token")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public ActionResult<TokenResponse> ExchangeToken(TokenRequest request)
    {
        var token = _authService.ExchangeSimpleToken(request.SimpleToken!);

        if (token is null)
        {
            return Unauthorized(ErrorResponse.InvalidSimpleToken());
        }

        return Ok(token);
    }

    /// <summary>
    /// Logs out by removing the current full token from the in-memory store.
    /// </summary>
    /// <param name="request">Full token and signature fields from the client.</param>
    /// <response code="200">The full token was removed.</response>
    /// <response code="400">The request body could not be read or is missing required data.</response>
    /// <response code="401">The full token or request signature is invalid.</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public IActionResult Logout(LogoutRequest request)
    {
        if (!_authService.Logout(request.FullToken!))
        {
            return Unauthorized(ErrorResponse.InvalidFullToken());
        }

        return Ok();
    }
}
