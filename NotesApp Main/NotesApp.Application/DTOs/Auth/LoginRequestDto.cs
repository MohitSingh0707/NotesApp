using System.ComponentModel.DataAnnotations;

namespace NotesApp.Application.DTOs.Auth;

public class LoginRequestDto
{
    [Required(ErrorMessage = "Email or Username is required")]
    public string Identifier { get; set; } = null!; 

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = null!;
}
