using NotesApp.Domain.Entities;

namespace NotesApp.Application.Interfaces.Common;

public interface IUserRepository
{
    // ---------------- CHECKS ----------------
    Task<bool> EmailExistsAsync(string email);
    Task<bool> UserNameExistsAsync(string userName);

    // ---------------- FETCH ----------------
    Task<User?> GetByEmailOrUserNameAsync(string identifier);
    Task<User?> GetByIdAsync(Guid id);

    // ---------------- PASSWORD RESET ----------------
    Task<User?> GetByResetTokenAsync(string hashedToken);

    // ---------------- COMMANDS ----------------
    Task AddAsync(User user);
    Task UpdateAsync(User user);
}
