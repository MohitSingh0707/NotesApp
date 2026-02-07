using System.ComponentModel.DataAnnotations;

namespace NotesApp.Application.DTOs.Auth;

public class LoginRequestDto
{
    [Required(ErrorMessage = "Email or Username is required")]
    public string Identifier { get; set; } = null!; 
    public string Password { get; set; } = null!;

    public string? FcmToken { get; set; }
    public string? Platform { get; set; }
}
