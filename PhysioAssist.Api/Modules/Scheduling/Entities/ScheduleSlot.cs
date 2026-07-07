namespace PhysioAssist.Api.Modules.Scheduling.Entities;

public class ScheduleSlot : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid DoctorId { get; set; }
    public Guid PatientId { get; set; }   // no longer nullable — see note below
    public DateTime SlotStart { get; set; }
    public DateTime SlotEnd { get; set; }
    public SlotStatus Status { get; set; }
}

