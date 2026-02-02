public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("Invalid credentials")
    {
    }

    public InvalidCredentialsException(string message)
        : base(message)
    {
    }
}
