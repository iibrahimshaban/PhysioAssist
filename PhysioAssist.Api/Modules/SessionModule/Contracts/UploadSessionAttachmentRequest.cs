namespace PhysioAssist.Api.Modules.SessionModule.Contracts
{
    public class UploadSessionAttachmentRequest
    {
        public ICollection<IFormFile> Files { get; set; } = [];
    }
}
