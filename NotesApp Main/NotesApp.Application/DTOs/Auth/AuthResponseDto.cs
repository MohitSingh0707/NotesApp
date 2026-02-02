namespace NotesApp.Application.DTOs.Auth;

public class AuthResponseDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
     public string UserName { get; set; } = null!;
    public string Token { get; set; } = null!;

    public string ProfileImageUrl { get; set; } = null!;
}
