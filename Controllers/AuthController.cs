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

    [HttpPost("login")]
    public ActionResult<TokenResponse> Login(LoginRequest request)
    {
        var token = _authService.Login(request.Login, request.Password);

        if (token is null)
        {
            return Unauthorized(ErrorResponse.InvalidCredentials());
        }

        return Ok(token);
    }

    [HttpPost("token")]
    public ActionResult<TokenResponse> Token(TokenRequest request)
    {
        var token = _authService.ExchangeSimpleToken(request.SimpleToken);

        if (token is null)
        {
            return Unauthorized(ErrorResponse.InvalidSimpleToken());
        }

        return Ok(token);
    }

    [HttpPost("logout")]
    public IActionResult Logout(LogoutRequest request)
    {
        if (!_authService.Logout(request.FullToken))
        {
            return Unauthorized(ErrorResponse.InvalidFullToken());
        }

        return Ok();
    }
}
