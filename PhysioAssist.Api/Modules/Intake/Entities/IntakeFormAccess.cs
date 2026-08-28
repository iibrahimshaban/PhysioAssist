namespace PhysioAssist.Api.Modules.Intake.Entities;

public class IntakeFormAccess
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid Nonce { get; set; }
    public Guid SchemaId { get; set; }
    public Guid ClinicId { get; set; }
    public Guid GeneratedByUserId { get; set; }  // receptionist / doctor / clinic admin who generated the QR
    public DateTime GeneratedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
