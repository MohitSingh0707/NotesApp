using Microsoft.AspNetCore.Mvc;
using NotesApp.Application.Common;

namespace NotesApp.API.Middleware;

public static class ValidationErrorResponse
{
    public static IActionResult Create(ActionContext context)
    {
        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .SelectMany(x => x.Value!.Errors)
            .Select(e =>
                string.IsNullOrWhiteSpace(e.ErrorMessage)
                    ? "Invalid value"
                    : e.ErrorMessage)
            .Distinct() // 🔥 duplicate messages remove
            .ToList();

        var response = FailureResponse.Create<object>(
            message: "Please fix the highlighted fields",
            statusCode: 400,
            errors: errors
        );

        return new BadRequestObjectResult(response);
    }
}
