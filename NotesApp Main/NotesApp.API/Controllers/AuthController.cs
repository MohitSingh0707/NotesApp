using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using NotesApp.Application.Common;
using NotesApp.Application.DTOs.Auth;
using NotesApp.Application.Interfaces.Auth;

namespace NotesApp.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IConfiguration _config;

    public AuthController(
        IAuthService authService,
        IJwtTokenGenerator jwtTokenGenerator,
        IGoogleAuthService googleAuthService,
        IConfiguration config)
    {
        _authService = authService;
        _jwtTokenGenerator = jwtTokenGenerator;
        _googleAuthService = googleAuthService;
        _config = config;
    }

    // ---------------- REGISTER ----------------
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var result = await _authService.RegisterAsync(request);

        return Ok(SuccessResponse.Create(
            data: result,
            message: "User registered successfully"
        ));
    }

    // ---------------- LOGIN ----------------
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);

        return Ok(SuccessResponse.Create(
            data: result,
            message: "Login successful"
        ));
    }

    // ---------------- GOOGLE LOGIN ----------------
    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            return BadRequest(FailureResponse.Create<object>(
                message: "Google token missing",
                statusCode: 400,
                errors: new List<string> { "IdToken is required" }
            ));
        }

        var user = await _googleAuthService
            .AuthenticateWithGoogleAsync(request.IdToken);

        // 🔥 IMPORTANT: mark as Google user (if not already)
        if (!user.IsRegisteredWithGoogle)
        {
            user.IsRegisteredWithGoogle = true;
            await _authService.MarkAsGoogleUserAsync(user.Id);
        }

        var token = _jwtTokenGenerator.GenerateToken(user);

        var profilePath = string.IsNullOrWhiteSpace(user.ProfileImagePath)
            ? _config["AWS:DefaultProfileImage"]
            : user.ProfileImagePath;

        var response = new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email ?? "",
            UserName = user.UserName ?? "",
            Token = token,
            ProfileImageUrl =
                _config["AWS:S3BaseUrl"] + profilePath
        };

        return Ok(SuccessResponse.Create(
            data: response,
            message: "Google login successful"
        ));
    }

    // ---------------- GUEST LOGIN ----------------
    [HttpPost("guest")]
    public async Task<IActionResult> GuestLogin()
    {
        var response = await _authService.GuestLoginAsync();

        return Ok(SuccessResponse.Create(
            data: response,
            message: "Guest login successful"
        ));
    }

    // ---------------- FORGOT PASSWORD ----------------
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequestDto request)
    {
        // 🔥 HARD BLOCK FOR GOOGLE USERS
        var isGoogleUser =
            await _authService.IsGoogleRegisteredUserAsync(request.Email);

        if (isGoogleUser)
        {
            return BadRequest(FailureResponse.Create<object>(
                message: "This account uses Google Sign-In. Password reset is not available.",
                statusCode: 400,
                errors: new List<string>
                {
                    "Please sign in using Google"
                }
            ));
        }

        await _authService.ForgotPasswordAsync(request);

        return Ok(SuccessResponse.Create<object>(
            data: null,
            message: "Password reset email sent"
        ));
    }

    // ---------------- RESET PASSWORD ----------------
    [Authorize]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequestDto request)
    {
        var userIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("Invalid token");

        await _authService.ResetPasswordAsync(userId, request);

        return Ok(SuccessResponse.Create<object>(
            data: null,
            message: "Password reset successful"
        ));
    }

    // ---------------- CHANGE PASSWORD ----------------
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequestDto request)
    {
        var userIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("Invalid token");

        await _authService.ChangePasswordAsync(userId, request);

        return Ok(SuccessResponse.Create<object>(
            data: null,
            message: "Password changed successfully"
        ));
    }

    // ---------------- CONVERT GUEST ----------------
    [Authorize]
    [HttpPost("convert-guest")]
    public async Task<IActionResult> ConvertGuest(
        [FromBody] ConvertGuestRequestDto request)
    {
        if (request == null)
            return BadRequest("Request body is required");

        var userIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("Invalid token");

        var response = await _authService
            .ConvertGuestAsync(userId, request);

        return Ok(SuccessResponse.Create(
            data: response,
            message: "Guest converted to registered user"
        ));
    }
}
