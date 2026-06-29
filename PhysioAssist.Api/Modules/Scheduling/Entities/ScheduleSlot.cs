namespace PhysioAssist.Api.Modules.Scheduling.Entities;

public class ScheduleSlot : AuditableEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid DoctorId { get; set; }
    public Guid? PatientId { get; set; }
    public DateTime SlotStart { get; set; }
    public DateTime SlotEnd { get; set; }
    public SlotStatus Status { get; set; } = SlotStatus.Available;
}
