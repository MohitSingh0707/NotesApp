using NotesApp.Domain.Entities;

namespace NotesApp.Application.Interfaces.Push
{
    public interface IDeviceTokenRepository
    {
        Task<DeviceToken?> GetAsync(Guid userId, string token);
        Task<List<DeviceToken>> GetByUserAsync(Guid userId);
        Task AddAsync(DeviceToken token);
        Task DeleteAsync(DeviceToken token);
    }
}
