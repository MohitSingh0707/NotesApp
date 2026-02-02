// namespace NotesApp.Application.Common;

// public static class FailureResponse
// {
//     public static ApiResponse<T> Create<T>(
//         string message,
//         int statusCode,
//         List<string>? errors = null)
//     {
//         return new ApiResponse<T>
//         {
//             Success = false,
//             StatusCode = statusCode,
//             Message = message,
//             Data = default,
//             Errors = errors
//         };
//     }
// }
namespace NotesApp.Application.Common;

public static class FailureResponse
{
    public static ApiResponse<T> Create<T>(
        string message,
        int statusCode,
        List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            StatusCode = statusCode,
            Message = message,
            Data = default,
            Errors = errors ?? new List<string>() // 🔥 always clean
        };
    }
}

