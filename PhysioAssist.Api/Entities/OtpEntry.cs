

namespace PhysioAssist.Api.Entities;

public class OtpEntry
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public int Code { get; set; }
    public OtpPurpose Purpose { get; set; }
    public DateTime ExpiresIn { get; set; }
    public bool IsExpired => DateTime.UtcNow > ExpiresIn;
    public bool IsActive { get; set; } = true;
}
