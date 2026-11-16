using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MiniIdentityApi.Application.DTOs.Auth;
using MiniIdentityApi.Application.Services;

namespace MiniIdentityApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthenticationService _authenticationService;

    public AuthController(AuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    // Only an Admin can create accounts (and optionally link them to a student).
    [Authorize(Roles = "Admin")]
    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        _authenticationService.Register(request);
        return StatusCode(StatusCodes.Status201Created, new { message = "User registered successfully." });
    }

    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public ActionResult<AuthResponse> Login([FromBody] LoginRequest request)
    {
        var response = _authenticationService.Login(request);
        return Ok(response);
    }
}