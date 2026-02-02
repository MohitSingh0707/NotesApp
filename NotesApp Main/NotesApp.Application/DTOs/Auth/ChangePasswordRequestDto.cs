using System.ComponentModel.DataAnnotations;
using NotesApp.Application.Common.Validation;

namespace NotesApp.Application.DTOs.Auth;

public class ChangePasswordRequestDto
{
    [Required]
    public string CurrentPassword { get; set; } = null!;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        ErrorMessage = "New password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one number and one special character")]
    public string NewPassword { get; set; } = null!;
}
