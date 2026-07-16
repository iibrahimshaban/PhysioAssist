using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using PhysioAssist.Api.Shared.Interfaces;
using PhysioAssist.Api.Shared.Interfaces.Common;
using QRCoder;
using System.Security.Cryptography;
using System.Text;
using IQRService = PhysioAssist.Api.Shared.Interfaces.IQRService;

namespace PhysioAssist.Api.Shared.QRService;

public class QRService(IOptions<QrSettings> options, IMediaStorageService mediaStorageService) : IQRService
{
    private readonly QrSettings _settings = options.Value;
    private readonly IMediaStorageService _mediaStorageService = mediaStorageService;

    public string GenerateSignedToken(Guid targetId, string purpose, TimeSpan? expiresIn = null)
    {
        var expiry = DateTimeOffset.UtcNow.Add(expiresIn ?? TimeSpan.FromDays(_settings.DefaultExpiryDays));
        var payload = $"{targetId}|{purpose}|{expiry.ToUnixTimeSeconds()}";
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        var signature = ComputeSignature(payloadBytes);

        var token = $"{Convert.ToBase64String(payloadBytes)}.{Convert.ToBase64String(signature)}";
        return Uri.EscapeDataString(token);
    }

    public bool TryValidateToken(string token, string expectedPurpose, out Guid targetId)
    {
        targetId = Guid.Empty;

        try
        {
            var decoded = Uri.UnescapeDataString(token);
            var parts = decoded.Split('.');

            if (parts.Length != 2)
                return false;

            var payloadBytes = Convert.FromBase64String(parts[0]);
            var signatureBytes = Convert.FromBase64String(parts[1]);

            var expectedSignature = ComputeSignature(payloadBytes);

            if (!CryptographicOperations.FixedTimeEquals(signatureBytes, expectedSignature))
                return false;

            var payload = Encoding.UTF8.GetString(payloadBytes);
            var segments = payload.Split('|');

            if (segments.Length != 3)
                return false;

            if (!Guid.TryParse(segments[0], out var id))
                return false;

            if (!string.Equals(segments[1], expectedPurpose, StringComparison.Ordinal))
                return false;

            if (!long.TryParse(segments[2], out var expiryUnixSeconds))
                return false;

            if (DateTimeOffset.UtcNow > DateTimeOffset.FromUnixTimeSeconds(expiryUnixSeconds))
                return false;

            targetId = id;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GenerateQrImageUrlAsync(string content, string folder, string publicId)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(20);

        using var stream = new MemoryStream(qrCodeBytes);
        var formFile = new FormFile(stream, 0, qrCodeBytes.Length, publicId, $"{publicId}.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        return await _mediaStorageService.UploadImageAsync(formFile, folder, publicId);
    }

    private byte[] ComputeSignature(byte[] payloadBytes)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.SecretKey));
        return hmac.ComputeHash(payloadBytes);
    }
}
