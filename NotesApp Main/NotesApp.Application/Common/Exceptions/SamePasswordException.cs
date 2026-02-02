namespace NotesApp.Application.Common.Exceptions;

public class SamePasswordException : AppException
{
    public SamePasswordException()
        : base("New password cannot be same as old password", 400)
    {
    }
}
