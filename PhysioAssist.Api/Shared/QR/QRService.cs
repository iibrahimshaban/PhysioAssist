using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PhysioAssist.Api.Shared.Interfaces.Common;
using IQRService = PhysioAssist.Api.Shared.Interfaces.Common.IQRService;

namespace PhysioAssist.Api.Shared.QR;

public class QRService(IOptions<QRTokenOptions> options, ILogger<QRService> logger) : IQRService
{
    private readonly QRTokenOptions _options = options.Value;
    private readonly ILogger<QRService> _logger = logger;

    public Result<string> GenerateToken(QRTokenPayload payload)
    {
        if (string.IsNullOrWhiteSpace(_options.SigningKey) || _options.SigningKey.Length < 32)
            return Result.Failure<string>(QRErrors.SigningKeyMissing);

        if (payload.TargetId == Guid.Empty || payload.Expiry <= DateTime.UtcNow || string.IsNullOrWhiteSpace(payload.Nonce))
            return Result.Failure<string>(QRErrors.InvalidToken);

        var signature = Sign(payload.Purpose, payload.TargetId, payload.Expiry, payload.Nonce);

        var signedPayload = new QRTokenPayload
        {
            Purpose = payload.Purpose,
            TargetId = payload.TargetId,
            Expiry = payload.Expiry,
            Nonce = payload.Nonce,
            Signature = signature
        };

        var json = JsonSerializer.Serialize(signedPayload);
        return Result.Success(ToBase64Url(Encoding.UTF8.GetBytes(json)));
    }

    public Result<QRTokenPayload> ValidateToken(string token, QRTokenPurpose expectedPurpose)
    {
        if (string.IsNullOrWhiteSpace(_options.SigningKey) || _options.SigningKey.Length < 32)
            return Result.Failure<QRTokenPayload>(QRErrors.SigningKeyMissing);

        if (string.IsNullOrWhiteSpace(token))
            return InvalidTokenResult();

        QRTokenPayload? payload;

        try
        {
            var json = Encoding.UTF8.GetString(FromBase64Url(token));
            payload = JsonSerializer.Deserialize<QRTokenPayload>(json);
        }
        catch (JsonException)
        {
            return InvalidTokenResult();
        }
        catch (FormatException)
        {
            return InvalidTokenResult();
        }

        if (payload is null || payload.TargetId == Guid.Empty || string.IsNullOrWhiteSpace(payload.Nonce) || string.IsNullOrWhiteSpace(payload.Signature))
            return InvalidTokenResult();

        if (payload.Purpose != expectedPurpose)
        {
            _logger.LogWarning("QR token validation failed because the purpose was {ActualPurpose} instead of {ExpectedPurpose}.", payload.Purpose, expectedPurpose);
            return Result.Failure<QRTokenPayload>(QRErrors.InvalidPurpose);
        }

        if (payload.Expiry <= DateTime.UtcNow)
        {
            _logger.LogWarning("QR token validation failed because the token expired at {Expiry}.", payload.Expiry);
            return Result.Failure<QRTokenPayload>(QRErrors.ExpiredToken);
        }

        var expectedSignature = Sign(payload.Purpose, payload.TargetId, payload.Expiry, payload.Nonce);

        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(payload.Signature), Encoding.UTF8.GetBytes(expectedSignature)))
            return InvalidTokenResult();

        return Result.Success(payload);
    }

    public Result<string> HashToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Result.Failure<string>(QRErrors.InvalidToken);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Result.Success(ToBase64Url(hash));
    }

    private Result<QRTokenPayload> InvalidTokenResult()
    {
        _logger.LogWarning("QR token validation failed because the token was invalid.");
        return Result.Failure<QRTokenPayload>(QRErrors.InvalidToken);
    }

    private string Sign(QRTokenPurpose purpose, Guid targetId, DateTime expiry, string nonce)
    {
        var signingInput = $"{(int)purpose}.{targetId:N}.{expiry.Ticks}.{nonce}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SigningKey));
        return ToBase64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes(signingInput)));
    }

    private static string ToBase64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] FromBase64Url(string value)
    {
        var padded = value
            .Replace('-', '+')
            .Replace('_', '/');

        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return Convert.FromBase64String(padded);
    }
}
