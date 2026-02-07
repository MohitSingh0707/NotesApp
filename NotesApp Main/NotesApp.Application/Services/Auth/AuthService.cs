using Microsoft.Extensions.Configuration;
using NotesApp.Application.Common.Exceptions;
using NotesApp.Application.DTOs.Auth;
using NotesApp.Application.Interfaces.Auth;
using NotesApp.Application.Interfaces.Common;
using NotesApp.Application.Interfaces.Emails;
using NotesApp.Application.Interfaces.Push;
using NotesApp.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace NotesApp.Application.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IEmailService _emailService;
    private readonly IDeviceTokenRepository _deviceTokenRepository;

    private readonly string _defaultProfileImage;
    private readonly string _s3BaseUrl;
    private readonly string _frontendResetUrl;

    public AuthService(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IEmailService emailService,
        IDeviceTokenRepository deviceTokenRepository,
        IConfiguration config)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _emailService = emailService;
        _deviceTokenRepository = deviceTokenRepository;

        _defaultProfileImage =
            config["AWS:DefaultProfileImage"] ?? "profile-images/default.png";

        _s3BaseUrl =
            config["AWS:S3BaseUrl"]
            ?? throw new Exception("AWS:S3BaseUrl is missing");

        _frontendResetUrl =
            config["Frontend:ResetPasswordUrl"]
            ?? throw new Exception("Frontend:ResetPasswordUrl is missing");
    }

    // ================= REGISTER =================
    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        // 🧹 SANITIZE & VALIDATE
        var firstName = request.FirstName?.Trim() ?? "";
        var lastName = request.LastName?.Trim() ?? "";
        var userName = request.UserName?.Trim() ?? "";
        var email = request.Email?.Trim().ToLower() ?? "";

        // Validate not empty after trim
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ValidationException("First name cannot be empty");
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ValidationException("Last name cannot be empty");
        if (string.IsNullOrWhiteSpace(userName))
            throw new ValidationException("Username cannot be empty");
        if (string.IsNullOrWhiteSpace(email))
            throw new ValidationException("Email cannot be empty");

        // Validate length limits
        if (firstName.Length > 50)
            throw new ValidationException("First name cannot exceed 50 characters");
        if (lastName.Length > 50)
            throw new ValidationException("Last name cannot exceed 50 characters");
        if (userName.Length > 20)
            throw new ValidationException("Username cannot exceed 20 characters");

        // Basic XSS prevention - reject HTML tags
        if (ContainsHtmlTags(firstName) || ContainsHtmlTags(lastName) || ContainsHtmlTags(userName))
            throw new ValidationException("Names and username cannot contain HTML or script tags");

        if (await _userRepository.EmailExistsAsync(email))
            throw new EmailAlreadyExistsException();

        if (await _userRepository.UserNameExistsAsync(userName))
            throw new UsernameAlreadyExistsException();

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            UserName = userName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            ProfileImagePath = _s3BaseUrl + _defaultProfileImage,
            IsGuest = false,
            IsRegisteredWithGoogle = false,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (!string.IsNullOrEmpty(request.FcmToken) && !string.IsNullOrEmpty(request.Platform))
        {
            await RegisterDeviceTokenAsync(user.Id, request.FcmToken, request.Platform);
        }

        return await BuildAuthResponse(user);
    }

    // ================= LOGIN =================
    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _userRepository
            .GetByEmailOrUserNameAsync(request.Identifier.Trim());

        // BLOCK DELETED USER
        if (user == null || user.IsDeleted)
            throw new InvalidCredentialsException();

        // Google users cannot login via password
        if (user.IsRegisteredWithGoogle)
            throw new InvalidCredentialsException("Please sign in using Google");

        bool isValid = false;
        bool isLegacy = false;

        // 1️ Try BCrypt (Modern)
        try 
        {
            if (!string.IsNullOrEmpty(user.PasswordHash) && user.PasswordHash.StartsWith("$2"))
            {
                isValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            }
        }
        catch { /* ignored */ }

        // 2️⃣ Try SHA256 (Legacy Fallback)
        if (!isValid)
        {
            var legacyHash = HashLegacyPassword(request.Password);
            if (user.PasswordHash == legacyHash)
            {
                isValid = true;
                isLegacy = true;
            }
        }

        if (!isValid)
            throw new InvalidCredentialsException();

        // TRANSPARENT MIGRATION: Update to BCrypt
        if (isLegacy)
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            Console.WriteLine($" User {user.Id} migrated to BCrypt on login.");
        }

        if (!string.IsNullOrEmpty(request.FcmToken) && !string.IsNullOrEmpty(request.Platform))
        {
            await RegisterDeviceTokenAsync(user.Id, request.FcmToken, request.Platform);
        }

        return await BuildAuthResponse(user);
    }

    // ================= GUEST LOGIN =================
    public async Task<GuestAuthResponseDto> GuestLoginAsync()
    {
        var guestUser = new User
        {
            Id = Guid.NewGuid(),
            IsGuest = true,
            IsDeleted = false,
            ProfileImagePath = _s3BaseUrl + _defaultProfileImage,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(guestUser);

        return new GuestAuthResponseDto
        {
            UserId = guestUser.Id,
            IsGuest = true,
            Token = _jwtTokenGenerator.GenerateToken(guestUser),
            ProfileImageUrl = guestUser.ProfileImagePath ?? (_s3BaseUrl + _defaultProfileImage),
            IsNotesUnlocked = false,
            RemainingAccessSeconds = 0,
            HasPushToken = false
        };
    }

    // ================= CONVERT GUEST =================
    public async Task<AuthResponseDto> ConvertGuestAsync(
        Guid guestUserId,
        ConvertGuestRequestDto request)
    {
        var user = await _userRepository.GetByIdAsync(guestUserId);

        if (user == null || user.IsDeleted || !user.IsGuest)
            throw new InvalidGuestUserException();

        if (await _userRepository.EmailExistsAsync(request.Email))
            throw new EmailAlreadyExistsException();

        if (await _userRepository.UserNameExistsAsync(request.UserName))
            throw new UsernameAlreadyExistsException();

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = request.Email.Trim().ToLower();
        user.UserName = request.UserName.Trim();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        user.IsGuest = false;
        user.IsRegisteredWithGoogle = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
        return await BuildAuthResponse(user);
    }

    // ================= FORGOT PASSWORD =================
    public async Task ForgotPasswordAsync(ForgotPasswordRequestDto request)
    {
        var user = await _userRepository
            .GetByEmailOrUserNameAsync(request.Email.Trim());

        if (user == null || user.IsDeleted)
            throw new UserNotFoundException();

        // 🛑 Block Google/Gmail users from resetting password
        bool isGoogleManaged = user.IsRegisteredWithGoogle || 
                               user.PasswordHash == null || 
                               (user.Email != null && user.Email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase));

        if (isGoogleManaged)
        {
            throw new ValidationException(
                "Aapka account Google Sign-In se juda hua hai. Password reset yahan se possible nahi hai. Kripya 'Login with Google' ka use karein.");
        }

        var jwtToken = _jwtTokenGenerator.GenerateToken(user);

        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        var resetLink =
            $"{_frontendResetUrl}?token={Uri.EscapeDataString(jwtToken)}";

        try
        {
            await _emailService.SendAsync(
                user.Email!,
                "Reset your password",
                BuildResetEmail(user.FirstName ?? "User", resetLink)
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ForgotPassword Email failed: {ex.Message}");
        }
    }

    // ================= RESET PASSWORD =================
    public async Task ResetPasswordAsync(
        Guid userId,
        ResetPasswordRequestDto request)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null || user.IsDeleted || user.IsGuest || 
            user.IsRegisteredWithGoogle || (user.Email != null && user.Email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase)))
            throw new ValidationException("Password reset is not available for Google/Gmail accounts.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
    }

    // ================= CHANGE PASSWORD =================
    public async Task ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequestDto request)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null || user.IsDeleted || user.IsGuest || 
            user.IsRegisteredWithGoogle || (user.Email != null && user.Email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase)))
            throw new ValidationException("Accounts with Google Sign-In or Gmail cannot change password here. Please manage your account via Google.");

        // Check Legacy or BCrypt
        bool isValid = false;
        if (user.PasswordHash != null && user.PasswordHash.StartsWith("$2"))
        {
             isValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash);
        }
        else 
        {
             isValid = (user.PasswordHash == HashLegacyPassword(request.CurrentPassword));
        }

        if (!isValid)
            throw new InvalidPasswordException("Current password is incorrect");

        // Check if new password is same as current password
        if (request.CurrentPassword == request.NewPassword)
            throw new ValidationException("New password must be different from current password");

        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        
        user.PasswordHash = newHash;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
    }

    // ================= GOOGLE HELPERS =================
    public async Task<bool> IsGoogleRegisteredUserAsync(string email)
    {
        var user = await _userRepository.GetByEmailOrUserNameAsync(email.Trim());
        if (user == null || user.IsDeleted) return false;
        
        return user.IsRegisteredWithGoogle || 
               user.PasswordHash == null ||
               (user.Email != null && user.Email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase));
    }

    public async Task MarkAsGoogleUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
            throw new UserNotFoundException();

        if (!user.IsRegisteredWithGoogle)
        {
            user.IsRegisteredWithGoogle = true;
            user.PasswordHash = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
        }
    }

    // ================= RESPONSE BUILDER =================
    private async Task<AuthResponseDto> BuildAuthResponse(User user)
    {
        var profilePath =
            string.IsNullOrWhiteSpace(user.ProfileImagePath)
                ? (_s3BaseUrl + _defaultProfileImage)
                : user.ProfileImagePath.StartsWith("http")
                    ? user.ProfileImagePath  // Already full URL
                    : (_s3BaseUrl + user.ProfileImagePath);  // Convert relative to full URL

        var now = DateTime.UtcNow;
        var isUnlocked = user.AccessibleTill.HasValue && user.AccessibleTill > now;
        var remainingSeconds = isUnlocked ? (long)(user.AccessibleTill!.Value - now).TotalSeconds : 0;
        
        // CHECK PUSH STATUS
        var tokens = await _deviceTokenRepository.GetByUserAsync(user.Id);
        var hasPushToken = tokens != null && tokens.Any();

        return new AuthResponseDto
        {
            UserId = user.Id,
            UserName = user.UserName ?? "",
            Email = user.Email ?? "",
            FirstName = user.FirstName ?? "",
            LastName = user.LastName ?? "",
            Token = _jwtTokenGenerator.GenerateToken(user),
            ProfileImageUrl = profilePath,
            IsCommonPasswordSet = !string.IsNullOrEmpty(user.CommonPasswordHash),
            IsNotesUnlocked = isUnlocked,
            RemainingAccessSeconds = remainingSeconds,
            HasPushToken = hasPushToken
        };
    }

    // ================= LEGACY HASH (SHA256) =================
    private static string HashLegacyPassword(string password)
    {
        using var sha = SHA256.Create();
        return Convert.ToBase64String(
            sha.ComputeHash(Encoding.UTF8.GetBytes(password)));
    }

    // ================= XSS PREVENTION HELPER =================
    private static bool ContainsHtmlTags(string input)
    {
        if (string.IsNullOrEmpty(input)) return false;
        return input.Contains("<") || input.Contains(">") || 
               input.Contains("script", StringComparison.OrdinalIgnoreCase) ||
               input.Contains("javascript:", StringComparison.OrdinalIgnoreCase);
    }

    // ================= EMAIL TEMPLATE =================
    private static string BuildResetEmail(string name, string link)
    {
        var year = DateTime.UtcNow.Year;
        return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Reset Your Password - NotesApp</title>
</head>
<body style='margin:0; padding:0; background-color:#f8fafc; font-family:""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f8fafc; padding:60px 0;'>
        <tr>
            <td align='center'>
                <table width='550' cellpadding='0' cellspacing='0' style='background:#ffffff; border-radius:24px; overflow:hidden; box-shadow:0 20px 40px rgba(0,0,0,0.06); border: 1px solid #e2e8f0;'>
                    <tr>
                        <td style='background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%); padding:45px 30px; text-align:center;'>
                            <h1 style='margin:0; color:#ffffff; font-size:32px; font-weight:800; letter-spacing:-1px;'>NotesApp</h1>
                            <p style='margin:10px 0 0; color:rgba(255,255,255,0.9); font-size:14px; font-weight:500; text-transform:uppercase; letter-spacing:2px;'>Security Update</p>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding:50px 45px; color:#1e293b;'>
                            <h2 style='margin:0 0 18px; font-size:24px; font-weight:700; color:#0f172a; letter-spacing:-0.5px;'>Hello {name},</h2>
                            <p style='margin:0 0 30px; font-size:16px; line-height:1.7; color:#475569;'>
                                We received a request to reset the password for your NotesApp account. No worries, we've got you covered! Just click the magic button below to set a new password.
                            </p>
                            
                            <div style='text-align:center; padding:5px 0 35px;'>
                                <a href='{link}' target='_blank' style='background:#4f46e5; color:#ffffff; padding:18px 40px; border-radius:14px; font-size:16px; font-weight:700; text-decoration:none; display:inline-block; box-shadow:0 10px 20px rgba(79, 70, 229, 0.25);'>
                                    Reset My Password
                                </a>
                            </div>

                            <p style='margin:0 0 20px; font-size:14px; line-height:1.6; color:#94a3b8; text-align:center;'>
                                If the button above doesn't work, copy and paste this link into your browser:<br/>
                                <span style='word-break:break-all; color:#4f46e5;'>{link}</span>
                            </p>

                            <p style='margin:0 0 20px; font-size:15px; line-height:1.6; color:#64748b;'>
                                <strong>Didn't request this?</strong> If you didn't ask for a password reset, you can safely ignore this email. Your account remains secure.
                            </p>
                            
                            <hr style='border:0; border-top:1px solid #f1f5f9; margin:30px 0;'>
                            
                            <p style='margin:0; font-size:13px; color:#94a3b8; text-align:center;'>
                                For security, this link will expire in 1 hour.
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style='background:#f8fafc; padding:30px; text-align:center; border-top:1px solid #f1f5f9;'>
                            <p style='margin:0; font-size:13px; color:#94a3b8;'>
                                &copy; {year} NotesApp. Crafted with ❤️ for productivity.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    public async Task RegisterDeviceTokenAsync(Guid userId, string token, string platform)
    {
        var existing = await _deviceTokenRepository.GetAsync(userId, token);
        if (existing == null)
        {
            await _deviceTokenRepository.AddAsync(new DeviceToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = token,
                Platform = platform.ToLower(),
                CreatedAt = DateTime.UtcNow
            });
            Console.WriteLine($"📱 FCM: Token registered for user {userId} during auth flow.");
        }
    }

    public async Task<bool> IsPushTokenRegisteredAsync(Guid userId)
    {
        var tokens = await _deviceTokenRepository.GetByUserAsync(userId);
        return tokens != null && tokens.Any();
    }
}
