<<<<<<< HEAD:PhysioAssist.Api/Shared/Interfaces/IMediaStorageService.cs
namespace PhysioAssist.Api.Shared.Interfaces;
=======
﻿namespace PhysioAssist.Api.Shared.Interfaces.Common;
>>>>>>> be94d86bf95f3c039134e9161e18565aa145bc99:PhysioAssist.Api/Shared/Interfaces/Common/IMediaStorageService.cs

public interface IMediaStorageService
{
    Task<string> UploadImageAsync(IFormFile file, string folder, string publicId);
    Task<string> UploadDocumentAsync(IFormFile file, string folder, string publicId);
    Task DeleteImageAsync(string publicId);
    Task DeleteImageByUrlAsync(string imageUrl);

    // ⚠️ جديدة - لرفع ملفات غير الصور (PDF). محتاجة إضافة الـ implementation
    // المطابقة في CloudinaryService.cs (باستخدام RawUploadParams بدل ImageUploadParams).
    Task<string> UploadRawFileAsync(Stream fileStream, string folder, string publicId, string fileExtension);
}
