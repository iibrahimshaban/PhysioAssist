namespace PhysioAssist.Api.Modules.InitialReportModule.Entities;

public class ReportAttachment
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ReportId { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public InitialReport Report { get; set; } = default!;
}
