namespace PhysioAssist.Api.Modules.Auth.Entities;

public class OtpEntry
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = default!;
    public string Code { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
}
