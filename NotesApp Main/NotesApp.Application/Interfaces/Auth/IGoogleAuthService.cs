using NotesApp.Domain.Entities;

namespace NotesApp.Application.Interfaces.Auth;

public interface IGoogleAuthService
{
    Task<User> AuthenticateWithGoogleAsync(string idToken);
}
