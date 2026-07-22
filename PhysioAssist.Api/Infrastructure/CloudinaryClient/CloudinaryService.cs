using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using PhysioAssist.Api.Shared.Interfaces.Common;

namespace PhysioAssist.Api.Infrastructure.CloudinaryClient;
public class CloudinaryService(Cloudinary cloudinary) : IMediaStorageService
{
    private readonly Cloudinary _cloudinary = cloudinary;
    public async Task<string> UploadImageAsync(IFormFile file, string folder, string publicId)
    {
        await using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            PublicId = publicId,
            Overwrite = true,
            Transformation = new Transformation()
                .Width(500).Height(500).Crop("fill").Gravity("auto")
        };
        var result = await _cloudinary.UploadAsync(uploadParams);
        if (result.Error != null)
            throw new InvalidOperationException(result.Error.Message);
        return result.SecureUrl.ToString();
    }
    public async Task<string> UploadClinicalImageAsync(IFormFile file, string folder, string publicId)
    {
        await using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            PublicId = publicId,
            Overwrite = true,
            Transformation = new Transformation()
                .Width(2000).Height(2000).Crop("limit")
        };
        var result = await _cloudinary.UploadAsync(uploadParams);
        if (result.Error != null)
            throw new InvalidOperationException(result.Error.Message);
        return result.SecureUrl.ToString();
    }
    //---------------------------------------------------------------------------------------------------
    public async Task DeleteImageAsync(string publicId)
    {
        var deleteParams = new DeletionParams(publicId);
        await _cloudinary.DestroyAsync(deleteParams);
    }
    //---------------------------------------------------------------------------------------------------
    public async Task DeleteImageByUrlAsync(string imageUrl)
    {
        var publicId = CloudinaryExtension.ExtractPublicId(imageUrl);
        if (string.IsNullOrEmpty(publicId))
            return;
        await DeleteImageAsync(publicId);
    }
    //---------------------------------------------------------------------------------------------------
    public async Task<string> UploadRawFileAsync(Stream fileStream, string folder, string publicId, string fileExtension)
    {
        var uploadParams = new RawUploadParams
        {
            File = new FileDescription($"{publicId}.{fileExtension}", fileStream),
            Folder = folder,
            PublicId = publicId,
            Overwrite = true
        };
        var result = await _cloudinary.UploadAsync(uploadParams);
        if (result.Error != null)
            throw new InvalidOperationException(result.Error.Message);
        return result.SecureUrl.ToString();
    }
}