namespace PhysioAssist.Api.Shared.Interfaces.Common;

public interface IMediaStorageService
{
    Task<string> UploadImageAsync(IFormFile file, string folder, string publicId);
    Task<string> UploadDocumentAsync(IFormFile file, string folder, string publicId);
    Task DeleteImageAsync(string publicId);
    Task DeleteImageByUrlAsync(string imageUrl);
}
