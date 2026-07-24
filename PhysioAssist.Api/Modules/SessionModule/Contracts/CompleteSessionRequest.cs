namespace PhysioAssist.Api.Modules.SessionModule.Contracts
{
    public class CompleteSessionRequest
    {
        public string EditedTranscript { get; set; } = string.Empty;
        public string? TreatmentPlanUpdated { get; set; }
        public ICollection<IFormFile> Files { get; set; } = [];
    }
}
