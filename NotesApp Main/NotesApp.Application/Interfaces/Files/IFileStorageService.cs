using System.IO;
using Microsoft.AspNetCore.Http;

namespace NotesApp.Application.Interfaces.Files;

public interface IFileStorageService
{
    Task<string> UploadProfileImageAsync(
        Stream fileStream,
        string contentType,
        Guid userId);

    Task<string> SaveImageAsync(IFormFile image);
    Task<string> SaveFileAsync(IFormFile file);

}
