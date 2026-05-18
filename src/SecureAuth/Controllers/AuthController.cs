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
    /// Validates user credentials and issues a short-lived simple token.
    /// </summary>
    /// <param name="request">Login credentials together with request signature metadata.</param>
    /// <response code="200">Simple token issued successfully.</response>
    /// <response code="400">Request body is invalid.</response>
    /// <response code="401">Credentials are invalid or request signature validation failed.</response>
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
    /// Exchanges a simple token for a longer-lived full token.
    /// </summary>
    /// <param name="request">Simple token together with request signature metadata.</param>
    /// <response code="200">Full token issued successfully.</response>
    /// <response code="400">Request body is invalid.</response>
    /// <response code="401">Simple token is invalid, expired, or request signature validation failed.</response>
    [HttpPost("token")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public ActionResult<TokenResponse> Token(TokenRequest request)
    {
        var token = _authService.ExchangeSimpleToken(request.SimpleToken!);

        if (token is null)
        {
            return Unauthorized(ErrorResponse.InvalidSimpleToken());
        }

        return Ok(token);
    }

    /// <summary>
    /// Invalidates an active full token.
    /// </summary>
    /// <param name="request">Full token together with request signature metadata.</param>
    /// <response code="200">Token removed successfully.</response>
    /// <response code="400">Request body is invalid.</response>
    /// <response code="401">Full token is invalid, expired, or request signature validation failed.</response>
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
