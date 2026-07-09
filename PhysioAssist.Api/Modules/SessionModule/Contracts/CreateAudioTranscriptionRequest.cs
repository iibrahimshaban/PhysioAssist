namespace PhysioAssist.Api.Modules.SessionModule.Contracts
{
    public class CreateAudioTranscriptionRequest
    {
        public IFormFile AudioFile { get; set; } = default!;
        public string? LanguageHint { get; set; }
        public string? Prompt { get; set; }
    }
}
