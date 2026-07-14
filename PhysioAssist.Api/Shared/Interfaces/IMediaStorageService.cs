namespace PhysioAssist.Api.Shared.Interfaces;

public interface IMediaStorageService
{
    Task<string> UploadImageAsync(IFormFile file, string folder, string publicId);
    Task DeleteImageAsync(string publicId);
    Task DeleteImageByUrlAsync(string imageUrl);
    Task<string> UploadAudioAsync(
    IFormFile file,
    string folder,
    string publicId);
}
