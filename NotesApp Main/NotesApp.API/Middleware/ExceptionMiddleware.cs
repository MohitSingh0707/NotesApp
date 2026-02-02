using NotesApp.Application.Common;
using NotesApp.Application.Common.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;

namespace NotesApp.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly JsonSerializerOptions _jsonOptions =
        new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        // ✅ All custom app exceptions
        catch (AppException ex)
        {
            await WriteErrorResponse(
                context,
                (HttpStatusCode)ex.StatusCode,
                ex.Message
            );
        }
        // ✅ Validation errors
        catch (ValidationException ex)
        {
            await WriteErrorResponse(
                context,
                HttpStatusCode.BadRequest,
                ex.Message
            );
        }
        // ✅ Unauthorized
        catch (UnauthorizedAccessException ex)
        {
            await WriteErrorResponse(
                context,
                HttpStatusCode.Unauthorized,
                ex.Message
            );
        }
        // ✅ Not found
        catch (NotFoundException ex)
        {
            await WriteErrorResponse(
                context,
                HttpStatusCode.NotFound,
                ex.Message
            );
        }
        // ❌ Truly unexpected errors only
        catch (Exception)
        {
            await WriteErrorResponse(
                context,
                HttpStatusCode.InternalServerError,
                "Something went wrong. Please try again later."
            );
        }
    }

    private static async Task WriteErrorResponse(
        HttpContext context,
        HttpStatusCode statusCode,
        string message)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = FailureResponse.Create<object>(
            message: message,
            statusCode: (int)statusCode,
            errors: new List<string>()
        );

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, _jsonOptions)
        );
    }
}
