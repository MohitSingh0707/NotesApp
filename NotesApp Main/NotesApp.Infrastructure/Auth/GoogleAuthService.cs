using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using NotesApp.Application.Interfaces.Auth;
using NotesApp.Application.Interfaces.Common;
using NotesApp.Domain.Entities;

namespace NotesApp.Infrastructure.Auth;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _config;

    public GoogleAuthService(
        IUserRepository userRepository,
        IConfiguration config)
    {
        _userRepository = userRepository;
        _config = config;
    }

    public async Task<User> AuthenticateWithGoogleAsync(string idToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
            throw new Exception("Google ID token is missing");

        GoogleJsonWebSignature.Payload payload;

        try
        {
            // ✅ BACKEND-ONLY GENERIC VALIDATION
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Google token validation failed");
            Console.WriteLine(ex);
            throw new Exception("Invalid Google token");
        }

        // ✅ Extra safety
        if (payload.Issuer != "accounts.google.com" &&
            payload.Issuer != "https://accounts.google.com")
        {
            throw new Exception("Invalid Google token issuer");
        }

        if (string.IsNullOrWhiteSpace(payload.Email))
            throw new Exception("Google account email not found");

        // 🔎 FIND USER
        var user = await _userRepository
            .GetByEmailOrUserNameAsync(payload.Email);

        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = payload.Email,

                // ✅ never null
                UserName = payload.Email.Split('@')[0],

                FirstName = payload.GivenName ?? payload.Name ?? "Google",
                LastName = payload.FamilyName ?? "",

                IsGuest = false,

                ProfileImagePath =
                    _config["AWS:DefaultProfileImage"]
                    ?? "profile-images/default.png",

                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);
        }

        return user;
    }
}
