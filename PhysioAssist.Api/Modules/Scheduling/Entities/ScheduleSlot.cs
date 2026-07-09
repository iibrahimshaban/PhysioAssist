namespace PhysioAssist.Api.Modules.Scheduling.Entities;

public class ScheduleSlot 
{
    public Guid Id { get; set; }
    public Guid DoctorId { get; set; }
    public Guid PatientId { get; set; }   // no longer nullable — see note below
    public DateTimeOffset SlotStart { get; set; }
    public DateTimeOffset SlotEnd { get; set; }
    public SlotStatus Status { get; set; }
}

