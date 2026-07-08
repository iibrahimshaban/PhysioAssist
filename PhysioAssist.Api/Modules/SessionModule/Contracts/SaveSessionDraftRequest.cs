namespace PhysioAssist.Api.Modules.SessionModule.Contracts
{
    public class SaveSessionDraftRequest
    {
        public string EditedTranscript { get; set; } = string.Empty;
        public ICollection<IFormFile> Files { get; set; } = [];
    }
}
