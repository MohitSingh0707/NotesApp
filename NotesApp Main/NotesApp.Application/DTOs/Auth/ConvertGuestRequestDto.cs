using System.ComponentModel.DataAnnotations;

namespace NotesApp.Application.DTOs.Auth;

public class ConvertGuestRequestDto
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string FirstName { get; set; } = null!;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string LastName { get; set; } = null!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [StringLength(20, MinimumLength = 3)]
    [RegularExpression(@"^[a-zA-Z0-9_\.]+$", ErrorMessage = "Username can only contain letters, numbers, underscores and dots")]
    public string UserName { get; set; } = null!;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = null!;
}
