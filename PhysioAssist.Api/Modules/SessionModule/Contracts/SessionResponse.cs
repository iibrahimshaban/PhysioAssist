namespace PhysioAssist.Api.Modules.SessionModule.Contracts
{
    public class SessionResponse
    {
        public Guid Id { get; set; }
        public string? Summary { get; set; }
        public SessionStatus Status { get; set; }
        public Guid DoctorId { get; set; }
        public Guid PatientId { get; set; }
        public Guid? ScheduleSlotId { get; set; }
    }
}
