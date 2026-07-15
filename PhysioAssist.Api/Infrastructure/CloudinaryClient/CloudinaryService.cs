using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
<<<<<<< HEAD
using PhysioAssist.Api.Shared.Interfaces;
=======
using PhysioAssist.Api.Shared.Interfaces.Common;

>>>>>>> be94d86bf95f3c039134e9161e18565aa145bc99
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
<<<<<<< HEAD
=======
    public async Task<string> UploadDocumentAsync(IFormFile file, string folder, string publicId)
    {
        await using var stream = file.OpenReadStream();

        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            PublicId = publicId,
            Overwrite = true
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.Error != null)
            throw new InvalidOperationException(result.Error.Message);

        return result.SecureUrl.ToString();
    }
>>>>>>> be94d86bf95f3c039134e9161e18565aa145bc99
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