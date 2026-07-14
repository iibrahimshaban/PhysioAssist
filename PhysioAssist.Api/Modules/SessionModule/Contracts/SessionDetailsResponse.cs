namespace PhysioAssist.Api.Modules.SessionModule.Contracts
{
    public class SessionDetailsResponse
    {
        public Guid Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime? SlotStart { get; set; }
        public DateTime? SlotEnd { get; set; }
        public int DurationInMinutes { get; set; }
        public SessionStatus Status { get; set; }
        public string? EditedTranscript { get; set; }
        public string? AudioFileUrl { get; set; }

        public List<SessionAttachmentResponse> Attachments { get; set; } = [];
    }
}
