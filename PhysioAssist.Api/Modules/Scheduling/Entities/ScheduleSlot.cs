namespace PhysioAssist.Api.Modules.Scheduling.Entities;

public class ScheduleSlot 
{
    public Guid Id { get; set; }
    public Guid DoctorId { get; set; }
    public Guid? PatientId { get; set; }
    public Guid? GuestId { get; set; }
    public Guest? Guest { get; set; }
    public DateTimeOffset SlotStart { get; set; }
    public DateTimeOffset SlotEnd { get; set; }
    public SlotStatus Status { get; set; }
    public Guid? PackageId { get; set; }
    public PatientSessionPackage? Package { get; set; }
}

