// namespace NotesApp.Application.Common;

// public static class SuccessResponse
// {
//     public static ApiResponse<T> Create<T>(
//         T data,
//         string message = "Request successful",
//         int statusCode = 200)
//     {
//         return new ApiResponse<T>
//         {
//             Success = true,
//             StatusCode = statusCode,
//             Message = message,
//             Data = data,
//             Errors = null
//         };
//     }
// }

namespace NotesApp.Application.Common;

public static class SuccessResponse
{
    public static ApiResponse<T> Create<T>(
        T data,
        string message = "Request successful",
        int statusCode = 200)
    {
        return new ApiResponse<T>
        {
            Success = true,
            StatusCode = statusCode,
            Message = message,
            Data = data,
            Errors = new List<string>() // 🔥 consistent shape
        };
    }
}
