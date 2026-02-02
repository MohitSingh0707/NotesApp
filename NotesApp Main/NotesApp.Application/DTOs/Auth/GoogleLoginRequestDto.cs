using System.ComponentModel.DataAnnotations;

namespace NotesApp.Application.DTOs.Auth;

public class GoogleLoginRequestDto
{
    [Required]
    public string IdToken { get; set; } = string.Empty;
}
