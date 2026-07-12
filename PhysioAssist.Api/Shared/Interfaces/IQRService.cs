using PhysioAssist.Api.Shared.QR;

namespace PhysioAssist.Api.Shared.Interfaces;

public interface IQRService
{
    Result<string> GenerateToken(QRTokenPayload payload);
    Result<QRTokenPayload> ValidateToken(string token, QRTokenPurpose expectedPurpose);
    Result<string> HashToken(string token);
}
