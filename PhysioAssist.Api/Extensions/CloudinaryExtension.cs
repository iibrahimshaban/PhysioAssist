namespace PhysioAssist.Api.Extensions;

public static class CloudinaryExtension
{
    public static string ExtractPublicId(string url)
    {
        try
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Split('/');

            var uploadIndex = Array.IndexOf(segments, "upload");
            if (uploadIndex < 0) return string.Empty;

            var afterUpload = segments.Skip(uploadIndex + 1).ToArray();

            if (afterUpload.Length > 0
                && afterUpload[0].StartsWith('v')
                && afterUpload[0].Length > 1
                && afterUpload[0][1..].All(char.IsDigit))
            {
                afterUpload = afterUpload.Skip(1).ToArray();
            }

            var publicIdWithExtension = string.Join("/", afterUpload);
            var dotIndex = publicIdWithExtension.LastIndexOf('.');

            return dotIndex >= 0
                ? publicIdWithExtension[..dotIndex]
                : publicIdWithExtension;
        }
        catch
        {
            return string.Empty;
        }
    }
}
