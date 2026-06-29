namespace PhysioAssist.Api.Modules.SessionModule.Entities;

public class Session : AuditableEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string? Summary { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Scheduled;
    public Guid DoctorId { get; set; }
    public Guid PatientId { get; set; }
    public Guid? ScheduleSlotId { get; set; }
    public SessionTranscription? Transcription { get; set; }
}
