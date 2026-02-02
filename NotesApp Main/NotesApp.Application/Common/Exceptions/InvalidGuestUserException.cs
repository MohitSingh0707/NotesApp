namespace NotesApp.Application.Common.Exceptions;

public class InvalidGuestUserException : AppException
{
    public InvalidGuestUserException()
        : base("Invalid guest user", 400)
    {
    }
}
