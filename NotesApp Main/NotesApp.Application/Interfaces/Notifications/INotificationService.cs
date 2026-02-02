namespace NotesApp.Application.Interfaces.Notifications
{
    public interface INotificationService
    {
        Task CreateAsync(
            Guid userId,
            string title,
            string message,
            string type,
            Guid? noteId = null,
            string? noteTitle = null
        );
    }
}
