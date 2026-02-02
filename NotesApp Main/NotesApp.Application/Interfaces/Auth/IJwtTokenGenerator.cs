using NotesApp.Domain.Entities;

namespace NotesApp.Application.Interfaces.Auth;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
