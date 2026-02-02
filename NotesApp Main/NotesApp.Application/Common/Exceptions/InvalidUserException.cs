namespace NotesApp.Application.Common.Exceptions;

public class InvalidUserException : AppException
{
    public InvalidUserException()
        : base("Invalid user", 400)
    {
    }
}
