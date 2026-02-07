using NotesApp.Application.Interfaces.Common;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using NotesApp.Application.Common.Exceptions;
using NotesApp.Application.DTOs.Auth;
using NotesApp.Application.Interfaces.Files;
using NotesApp.Application.Interfaces.Users;
using NotesApp.Application.Interfaces.Auth;
using NotesApp.Application.Interfaces.Push;

namespace NotesApp.Application.Services.Users;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IDeviceTokenRepository _deviceTokenRepository;
    private readonly string _s3BaseUrl;

    public UserService(
        IUserRepository userRepository, 
        IConfiguration config,
        IFileStorageService fileStorageService,
        IDeviceTokenRepository deviceTokenRepository)
    {
        _userRepository = userRepository;
        _s3BaseUrl = config["AWS:S3BaseUrl"]!;
        _fileStorageService = fileStorageService;
        _deviceTokenRepository = deviceTokenRepository;
    }

    // ✅ GET CURRENT USER
    public async Task<UserProfileDto> GetCurrentUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null || user.IsDeleted)
            throw new NotFoundException("User not found");

        var profilePath = string.IsNullOrWhiteSpace(user.ProfileImagePath) 
            ? (_s3BaseUrl + "profile-images/default.png") 
            : user.ProfileImagePath.StartsWith("http") 
                ? user.ProfileImagePath  // Already full URL
                : (_s3BaseUrl + user.ProfileImagePath);  // Convert relative to full URL

        var now = DateTime.UtcNow;
        var isUnlocked = user.AccessibleTill.HasValue && user.AccessibleTill.Value > now;
        var remainingSeconds = user.AccessibleTill.HasValue 
            ? (long)Math.Max(0, (user.AccessibleTill.Value - now).TotalSeconds)
            : 0;

        var tokens = await _deviceTokenRepository.GetByUserAsync(userId);

        return new UserProfileDto
        {
            UserId = user.Id,
            FirstName = user.FirstName ?? "",
            LastName = user.LastName ?? "",
            UserName = user.UserName ?? "",
            Email = user.Email ?? "",
            ProfileImageUrl = profilePath,
            IsCommonPasswordAvailable = !string.IsNullOrWhiteSpace(user.CommonPasswordHash),
            IsNotesUnlocked = isUnlocked,
            RemainingAccessSeconds = remainingSeconds,
            HasPushToken = tokens != null && tokens.Any()
        };
    }

    // ✅ UPDATE PROFILE
    public async Task UpdateUserProfileAsync(
        Guid userId,
        UpdateUserProfileDto request)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null || user.IsDeleted)
            throw new NotFoundException("User not found");

        // 🧹 SANITIZE & UPDATE FIRST NAME
        if (!string.IsNullOrWhiteSpace(request.FirstName))
        {
            var cleanFirstName = request.FirstName.Trim();
            if (cleanFirstName.Length > 50) cleanFirstName = cleanFirstName.Substring(0, 50);
            user.FirstName = cleanFirstName;
        }

        // 🧹 SANITIZE & UPDATE LAST NAME
        if (!string.IsNullOrWhiteSpace(request.LastName))
        {
            var cleanLastName = request.LastName.Trim();
            if (cleanLastName.Length > 50) cleanLastName = cleanLastName.Substring(0, 50);
            user.LastName = cleanLastName;
        }

        // 🧹 SANITIZE & UPDATE USERNAME
        if (!string.IsNullOrWhiteSpace(request.UserName))
        {
            var cleanUserName = request.UserName.Trim();
            if (cleanUserName.Length > 20) cleanUserName = cleanUserName.Substring(0, 20);

            // Check if username is being changed and if it's unique
            if (cleanUserName != user.UserName)
            {
                var isTaken = await _userRepository.UserNameExistsAsync(cleanUserName);
                if (isTaken)
                    throw new ValidationException("Username is already taken");
                
                user.UserName = cleanUserName;
            }
        }
        
        // Handle Profile Image Logic (only if provided)
        if (!string.IsNullOrEmpty(request.ProfileImagePath))
        {
             user.ProfileImagePath = request.ProfileImagePath;
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
    }

    // ✅ SOFT DELETE USER
    public async Task DeleteUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null || user.IsDeleted)
            throw new NotFoundException("User not found");

        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
    }

    public async Task ChangeCommonPasswordAsync(
        Guid userId,
        string oldPassword,
        string newPassword)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null || user.IsDeleted)
            throw new NotFoundException("User not found");

        if (string.IsNullOrWhiteSpace(user.CommonPasswordHash))
            throw new ValidationException("Common password not set");

        var isValidOld =
            BCrypt.Net.BCrypt.Verify(oldPassword, user.CommonPasswordHash);

        if (!isValidOld)
            throw new UnauthorizedAccessException("Old password is incorrect");

        // Check if new password is same as old password
        if (oldPassword == newPassword)
            throw new ValidationException("New password must be different from old password");

        // Use standard validation for strength if needed (already handled by DTO if through controller)
        if (newPassword.Length < 8)
            throw new ValidationException("New password must be at least 8 characters");

        user.CommonPasswordHash =
            BCrypt.Net.BCrypt.HashPassword(newPassword);

        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
    }



    public async Task<string> UploadProfileImageAsync(Guid userId, Stream fileStream, string contentType)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
            throw new NotFoundException("User not found");

        // Upload to S3 (Now returns full URL)
        var fullUrl = await _fileStorageService.UploadProfileImageAsync(fileStream, contentType, userId);
        
        user.ProfileImagePath = fullUrl;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);

        return fullUrl;
    }
}
