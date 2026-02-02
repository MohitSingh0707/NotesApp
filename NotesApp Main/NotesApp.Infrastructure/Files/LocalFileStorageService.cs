// using System.IO;
// using Microsoft.AspNetCore.Http;
// using NotesApp.Application.Interfaces.Files;

// namespace NotesApp.Infrastructure.Files;

// public class LocalFileStorageService : IFileStorageService
// {
//     private readonly string _imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
//     private readonly string _filesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "files");

//     public LocalFileStorageService()
//     {
//         Directory.CreateDirectory(_imagesPath);
//         Directory.CreateDirectory(_filesPath);
//     }

//     public Task<string> UploadProfileImageAsync(Stream fileStream, string contentType, Guid userId)
//     {
//         // Implementation for profile image upload
//         var fileName = $"{userId}.jpg"; // or based on contentType
//         var filePath = Path.Combine(_imagesPath, fileName);
//         using (var fileStreamDest = new FileStream(filePath, FileMode.Create))
//         {
//             fileStream.CopyTo(fileStreamDest);
//         }
//         return Task.FromResult($"/images/{fileName}");
//     }

//     public async Task<string> SaveImageAsync(IFormFile image)
//     {
//         var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
//         var filePath = Path.Combine(_imagesPath, fileName);
//         using (var stream = new FileStream(filePath, FileMode.Create))
//         {
//             await image.CopyToAsync(stream);
//         }
//         return $"/images/{fileName}";
//     }

//     public async Task<string> SaveFileAsync(IFormFile file)
//     {
//         var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
//         var filePath = Path.Combine(_filesPath, fileName);
//         using (var stream = new FileStream(filePath, FileMode.Create))
//         {
//             await file.CopyToAsync(stream);
//         }
//         return $"/files/{fileName}";
//     }
// }
