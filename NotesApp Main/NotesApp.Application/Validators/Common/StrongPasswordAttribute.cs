using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace NotesApp.Application.Common.Validation;

public class StrongPasswordAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext validationContext)
    {
        var password = value as string;

        if (string.IsNullOrWhiteSpace(password))
            return new ValidationResult("Password is required");

        var regex = new Regex(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).{8,}$");

        if (!regex.IsMatch(password))
        {
            return new ValidationResult(
                "Password must be at least 8 characters long and include uppercase, lowercase, number, and special character");
        }

        return ValidationResult.Success;
    }
}
