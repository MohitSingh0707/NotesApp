using NotesApp.Application.DTOs.Auth;

namespace NotesApp.Application.Interfaces.Users;

public interface IUserService
{
    Task<UserProfileDto> GetCurrentUserAsync(Guid userId);
    Task UpdateUserProfileAsync(Guid userId, UpdateUserProfileDto request);
    Task DeleteUserAsync(Guid userId);
    // 🔥 ADD THIS
    Task ChangeCommonPasswordAsync(
        Guid userId,
        string oldPassword,
        string newPassword);

    Task<string> UploadProfileImageAsync(Guid userId, Stream fileStream, string contentType);
}
