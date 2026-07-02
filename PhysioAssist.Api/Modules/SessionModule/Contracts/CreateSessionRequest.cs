namespace PhysioAssist.Api.Modules.SessionModule.Contracts
{
    public class CreateSessionRequest
    {
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid? ScheduleSlotId { get; set; }
    }
}
