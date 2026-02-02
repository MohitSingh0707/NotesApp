namespace NotesApp.Application.Interfaces.Push
{
    public interface IPushNotificationService
    {
        Task SendAsync(Guid userId, string title, string body);
    }
}
