using NotesApp.Application.Interfaces.Common;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using NotesApp.Application.Common.Exceptions;
using NotesApp.Application.DTOs.Auth;

namespace NotesApp.Application.Services.Users;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly string _s3BaseUrl;

    public UserService(IUserRepository userRepository, IConfiguration config)
    {
        _userRepository = userRepository;
        _s3BaseUrl = config["AWS:S3BaseUrl"]!;
    }

    // ✅ GET CURRENT USER
    public async Task<UserProfileDto> GetCurrentUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null || user.IsDeleted)
            throw new NotFoundException("User not found");

        var profilePath = string.IsNullOrWhiteSpace(user.ProfileImagePath) ? "" : user.ProfileImagePath;

        return new UserProfileDto
        {
            UserId = user.Id,
            FirstName = user.FirstName ?? "",
            LastName = user.LastName ?? "",
            UserName = user.UserName ?? "",
            Email = user.Email ?? "",
            ProfileImageUrl = _s3BaseUrl + (profilePath.StartsWith("http") ? "" : profilePath)
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

        // 🧹 SANITIZE
        var cleanFirstName = request.FirstName.Trim();
        if (cleanFirstName.Length > 50) cleanFirstName = cleanFirstName.Substring(0, 50);

        var cleanLastName = request.LastName.Trim();
        if (cleanLastName.Length > 50) cleanLastName = cleanLastName.Substring(0, 50);

        var cleanUserName = request.UserName.Trim().ToLower();
        if (cleanUserName.Length > 20) cleanUserName = cleanUserName.Substring(0, 20);

        // Check if username is being changed and if it's unique
        if (cleanUserName != user.UserName)
        {
            var isTaken = await _userRepository.UserNameExistsAsync(cleanUserName);
            if (isTaken)
                throw new ValidationException("Username is already taken");
        }

        user.FirstName = cleanFirstName;
        user.LastName = cleanLastName;
        user.UserName = cleanUserName;
        
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

        // Use standard validation for strength if needed (already handled by DTO if through controller)
        if (newPassword.Length < 8)
            throw new ValidationException("New password must be at least 8 characters");

        user.CommonPasswordHash =
            BCrypt.Net.BCrypt.HashPassword(newPassword);

        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
    }



}
