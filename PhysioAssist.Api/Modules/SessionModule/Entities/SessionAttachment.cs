namespace PhysioAssist.Api.Modules.SessionModule.Entities
{
    public class SessionAttachment: AuditableEntity
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();
        public Guid SessionId { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public Session Session { get; set; } = default!;
    }
}
