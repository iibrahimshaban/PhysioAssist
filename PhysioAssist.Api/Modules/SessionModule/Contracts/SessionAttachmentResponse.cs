namespace PhysioAssist.Api.Modules.SessionModule.Contracts
{
    public class SessionAttachmentResponse
    {
        public Guid Id { get; set; }

        public string FileUrl { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public string FileType { get; set; } = string.Empty;
    }
}
