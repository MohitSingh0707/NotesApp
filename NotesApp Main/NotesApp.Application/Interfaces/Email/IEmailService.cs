using System.Threading.Tasks;

namespace NotesApp.Application.Interfaces.Emails
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body);
    }
}
