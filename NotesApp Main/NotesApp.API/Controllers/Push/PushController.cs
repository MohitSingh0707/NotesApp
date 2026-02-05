using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotesApp.Application.Common;
using NotesApp.Application.Common.Extensions;
using NotesApp.Application.DTOs.Push;
using NotesApp.Application.Interfaces.Push;
using NotesApp.Domain.Entities;

namespace NotesApp.API.Controllers.Push
{
    [Authorize]
    [ApiController]
    [Route("api/push")]
    public class PushController : ControllerBase
    {
        private readonly IDeviceTokenRepository _deviceTokenRepository;

        public PushController(IDeviceTokenRepository deviceTokenRepository)
        {
            _deviceTokenRepository = deviceTokenRepository;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterDeviceTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return BadRequest(FailureResponse.Create<object>(
                    message: "Device token is required",
                    statusCode: 400
                ));
            }

            // Validate token length (FCM tokens are typically 140-200 characters)
            if (request.Token.Length < 10 || request.Token.Length > 250)
            {
                return BadRequest(FailureResponse.Create<object>(
                    message: "Invalid device token length",
                    statusCode: 400,
                    errors: new List<string> { $"Token length: {request.Token.Length}, Expected: 10-250 characters" }
                ));
            }

            // Validate platform
            var validPlatforms = new[] { "Android", "iOS", "Web" };
            if (!string.IsNullOrWhiteSpace(request.Platform) && 
                !validPlatforms.Contains(request.Platform, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(FailureResponse.Create<object>(
                    message: "Invalid platform",
                    statusCode: 400,
                    errors: new List<string> { $"Platform must be one of: {string.Join(", ", validPlatforms)}" }
                ));
            }

            var userId = User.GetUserId();

            var existing =
                await _deviceTokenRepository.GetAsync(userId, request.Token);

            if (existing == null)
            {
                await _deviceTokenRepository.AddAsync(new DeviceToken
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Token = request.Token,
                    Platform = request.Platform,
                    CreatedAt = DateTime.UtcNow
                });
            }

            return Ok(SuccessResponse.Create<object>(
                data: null,
                message: "Device token registered successfully"
            ));
        }
    }
}
