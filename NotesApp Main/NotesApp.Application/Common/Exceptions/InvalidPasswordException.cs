namespace NotesApp.Application.Common.Exceptions;

public class InvalidPasswordException : AppException
{
    public InvalidPasswordException(string message)
        : base(message, 400)
    {
    }
}
