namespace PhysioAssist.Api.Shared.Interfaces;

public interface IMediaStorageService
{
    Task<string> UploadImageAsync(IFormFile file, string folder, string publicId);
    Task DeleteImageAsync(string publicId);
    Task DeleteImageByUrlAsync(string imageUrl);

    // ⚠️ جديدة - لرفع ملفات غير الصور (PDF). محتاجة إضافة الـ implementation
    // المطابقة في CloudinaryService.cs (باستخدام RawUploadParams بدل ImageUploadParams).
    Task<string> UploadRawFileAsync(Stream fileStream, string folder, string publicId, string fileExtension);
}
