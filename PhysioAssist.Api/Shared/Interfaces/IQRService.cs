namespace PhysioAssist.Api.Shared.Interfaces;

public interface IQRService
{
    /// <summary>
    /// Generates a signed, tamper-proof token that encodes a target entity id and a purpose.
    /// </summary>
    string GenerateSignedToken(Guid targetId, string purpose, TimeSpan? expiresIn = null);

    /// <summary>
    /// Validates a previously generated signed token. Returns false if the signature is invalid,
    /// the purpose does not match, or the token has expired.
    /// </summary>
    bool TryValidateToken(string token, string expectedPurpose, out Guid targetId);

    /// <summary>
    /// Renders the given content as a QR code PNG image and uploads it, returning the public URL.
    /// </summary>
    Task<string> GenerateQrImageUrlAsync(string content, string folder, string publicId);
}
