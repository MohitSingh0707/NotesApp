namespace NotesApp.Application.Common.Exceptions;

public class EmailAlreadyExistsException : AppException
{
    public EmailAlreadyExistsException()
        : base("Email already exists", 409)
    {
    }
}
