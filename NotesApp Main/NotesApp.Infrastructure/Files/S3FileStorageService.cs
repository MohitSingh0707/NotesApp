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
    private readonly string _s3BaseUrl;

    public S3FileStorageService(
        IAmazonS3 s3,
        IConfiguration config)
    {
        _s3 = s3;
        _bucket = config["AWS:BucketName"]!;
        _s3BaseUrl = config["AWS:S3BaseUrl"] ?? throw new Exception("AWS:S3BaseUrl is missing");
    }

    public async Task<string> SaveFileAsync(IFormFile file)
    {
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var key = $"notes-files/{fileName}";

        using var stream = file.OpenReadStream();
        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = stream,
            ContentType = file.ContentType
        };

        await _s3.PutObjectAsync(request);
        return GenerateFullUrl(key);
    }

    public async Task<string> SaveImageAsync(IFormFile image)
    {
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
        var key = $"notes-images/{fileName}";

        using var stream = image.OpenReadStream();
        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = stream,
            ContentType = image.ContentType
        };

        await _s3.PutObjectAsync(request);
        return GenerateFullUrl(key);
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

        return GenerateFullUrl(key);
    }

    private string GenerateFullUrl(string key)
    {
        var baseUrl = _s3BaseUrl.TrimEnd('/');
        var relativePath = key.TrimStart('/');
        return $"{baseUrl}/{relativePath}";
    }
}
