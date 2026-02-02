using Microsoft.EntityFrameworkCore;
using NotesApp.Application.Interfaces.Push;
using NotesApp.Domain.Entities;

namespace NotesApp.Infrastructure.Persistence.Repositories.Push
{
    public class DeviceTokenRepository : IDeviceTokenRepository
    {
        private readonly AppDbContext _context;

        public DeviceTokenRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DeviceToken?> GetAsync(Guid userId, string token)
        {
            return await _context.DeviceTokens
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Token == token);
        }

        public async Task<List<DeviceToken>> GetByUserAsync(Guid userId)
        {
            return await _context.DeviceTokens
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }

        public async Task AddAsync(DeviceToken token)
        {
            await _context.DeviceTokens.AddAsync(token);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(DeviceToken token)
        {
            _context.DeviceTokens.Remove(token);
            await _context.SaveChangesAsync();
        }
    }
}
