using Microsoft.EntityFrameworkCore;
using global::NotesApp.Application.Interfaces.Common;
using global::NotesApp.Domain.Entities;
using global::NotesApp.Infrastructure.Persistence;

namespace NotesApp.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users.AnyAsync(x => x.Email == email);
    }

    public async Task<bool> UserNameExistsAsync(string userName)
    {
        return await _context.Users.AnyAsync(x => x.UserName == userName);
    }

    public async Task<User?> GetByEmailOrUserNameAsync(string identifier)
    {
        // Email is case-insensitive, Username is case-sensitive
        return await _context.Users.FirstOrDefaultAsync(u =>
            u.Email == identifier || 
            EF.Functions.Collate(u.UserName, "SQL_Latin1_General_CP1_CS_AS") == EF.Functions.Collate(identifier, "SQL_Latin1_General_CP1_CS_AS"));
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users.FindAsync(id);
    }

    // 🔥 REQUIRED FOR RESET PASSWORD
    public async Task<User?> GetByResetTokenAsync(string hashedToken)
    {
        return await _context.Users.FirstOrDefaultAsync(u =>
            u.PasswordResetToken == hashedToken &&
            u.PasswordResetTokenExpiry > DateTime.UtcNow
        );
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public Task SoftDeleteAsync(User user)
    {
        throw new NotImplementedException();
    }
}
