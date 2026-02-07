using NotesApp.Application.DTOs.Auth;

namespace NotesApp.Application.Interfaces.Auth
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
        Task<GuestAuthResponseDto> GuestLoginAsync();
        Task<AuthResponseDto> ConvertGuestAsync(Guid guestUserId, ConvertGuestRequestDto request);

        Task ForgotPasswordAsync(ForgotPasswordRequestDto request);
        Task ResetPasswordAsync(Guid userId, ResetPasswordRequestDto request);
        Task ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request);

        // ✅ ADD THESE (FOR GOOGLE FLOW)
        Task<bool> IsGoogleRegisteredUserAsync(string email);
        Task MarkAsGoogleUserAsync(Guid userId);
        Task RegisterDeviceTokenAsync(Guid userId, string token, string platform);
        Task<bool> IsPushTokenRegisteredAsync(Guid userId);
    }
}
