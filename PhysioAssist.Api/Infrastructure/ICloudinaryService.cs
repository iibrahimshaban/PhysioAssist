namespace PhysioAssist.Api.Infrastructure;

public interface ICloudinaryService
{
    Task<string> UploadImageAsync(IFormFile file, string folder, string publicId);
    Task DeleteImageAsync(string publicId);
    Task DeleteImageByUrlAsync(string imageUrl);
}
