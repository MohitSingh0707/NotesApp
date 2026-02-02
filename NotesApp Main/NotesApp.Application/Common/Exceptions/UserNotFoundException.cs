namespace NotesApp.Application.Common.Exceptions;

public class UserNotFoundException : AppException
{
    public UserNotFoundException()
        : base("User not found", 404)
    {
    }
}
