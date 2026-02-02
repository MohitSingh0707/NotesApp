namespace NotesApp.Application.Common.Exceptions;

public class UsernameAlreadyExistsException : AppException
{
    public UsernameAlreadyExistsException()
        : base("Username already exists", 409)
    {
    }
}
