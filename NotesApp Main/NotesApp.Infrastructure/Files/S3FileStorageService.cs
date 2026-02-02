using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using NotesApp.Application.Interfaces.Files;

namespace NotesApp.Infrastructure.Files;

public class S3FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;

    public S3FileStorageService(
        IAmazonS3 s3,
        IConfiguration config)
    {
        _s3 = s3;
        _bucket = config["AWS:BucketName"]!;
    }

    public Task<string> SaveFileAsync(IFormFile file)
    {
        throw new NotImplementedException();
    }

    public Task<string> SaveImageAsync(IFormFile image)
    {
        throw new NotImplementedException();
    }

    // ✅ ONLY METHOD REQUIRED BY INTERFACE
    public async Task<string> UploadProfileImageAsync(
        Stream fileStream,
        string contentType,
        Guid userId)
    {
        var key = $"profile-images/users/{userId}.jpg";

        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = fileStream,
            ContentType = contentType
        };

        await _s3.PutObjectAsync(request);

        return key; // relative path only
    }
}
