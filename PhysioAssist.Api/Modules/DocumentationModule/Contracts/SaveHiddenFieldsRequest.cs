namespace PhysioAssist.Api.Modules.DocumentationModule.Contracts;

public sealed class SaveHiddenFieldsRequest
{
    public List<string> HiddenFieldIds { get; set; } = [];
}
