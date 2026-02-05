using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using NotesApp.Application.Common;
using NotesApp.Application.DTOs.Auth;
using NotesApp.Application.Interfaces.Common;
using NotesApp.Application.Interfaces.Users;

[Authorize]
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    // ================= HELPER =================
    private Guid GetUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(idClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid or missing user id");

        return userId;
    }

    // ================= GET CURRENT USER =================
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = GetUserId();

        var user = await _userService.GetCurrentUserAsync(userId);

        return Ok(SuccessResponse.Create(
            data: user,
            message: "User fetched successfully"
        ));
    }

    // ================= UPDATE PROFILE =================
    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateUserProfileDto request)
    {
        var userId = GetUserId();

        await _userService.UpdateUserProfileAsync(userId, request);

        return Ok(SuccessResponse.Create<object>(
            data: null,
            message: "Profile updated successfully"
        ));
    }

    // ================= DELETE USER (SOFT DELETE) =================
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = GetUserId();

        await _userService.DeleteUserAsync(userId);

        return Ok(SuccessResponse.Create<object>(
            data: null,
            message: "Account deleted successfully"
        ));
    }

    // ================= CHANGE COMMON PASSWORD =================
    [HttpPost("change-common-password")]
    public async Task<IActionResult> ChangeCommonPassword(
        [FromBody] ChangeCommonPasswordRequest request)
    {
        var userId = GetUserId();

        await _userService.ChangeCommonPasswordAsync(
            userId,
            request.OldPassword,
            request.NewPassword
        );

        return Ok(SuccessResponse.Create<object>(
            data: null,
            message: "Common password updated successfully"
        ));
    }

    // ================= UPLOAD PROFILE IMAGE =================
    [HttpPost("me/profile-image")]
    public async Task<IActionResult> UploadProfileImage(IFormFile file)
    {
        // Validate file exists
        if (file == null || file.Length == 0)
            return BadRequest(FailureResponse.Create<object>("No file uploaded", 400));

        // Validate file size (5MB limit)
        const long maxFileSize = 5 * 1024 * 1024; // 5MB
        if (file.Length > maxFileSize)
            return BadRequest(FailureResponse.Create<object>(
                message: "File size exceeds 5MB limit",
                statusCode: 400,
                errors: new List<string> { $"File size: {file.Length / 1024 / 1024}MB, Maximum allowed: 5MB" }
            ));

        // Validate content type (images only)
        var allowedContentTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
        if (!allowedContentTypes.Contains(file.ContentType.ToLower()))
            return BadRequest(FailureResponse.Create<object>(
                message: "Invalid file type",
                statusCode: 400,
                errors: new List<string> { $"Only image files are allowed (JPEG, PNG, GIF, WebP). Received: {file.ContentType}" }
            ));

        // Validate file extension
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var fileExtension = Path.GetExtension(file.FileName)?.ToLower();
        if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
            return BadRequest(FailureResponse.Create<object>(
                message: "Invalid file extension",
                statusCode: 400,
                errors: new List<string> { $"Only image file extensions are allowed (.jpg, .jpeg, .png, .gif, .webp)" }
            ));

        var userId = GetUserId();

        using var stream = file.OpenReadStream();
        var fullUrl = await _userService.UploadProfileImageAsync(userId, stream, file.ContentType);

        return Ok(SuccessResponse.Create(
            data: new { profileImageUrl = fullUrl },
            message: "Profile image uploaded successfully"
        ));
    }
}
