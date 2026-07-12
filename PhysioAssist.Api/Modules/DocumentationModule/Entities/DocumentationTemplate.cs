namespace PhysioAssist.Api.Modules.DocumentationModule.Entities;

public class DocumentationTemplate 
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public PatientCategory Category { get; set; }
    public string Name { get; set; } = string.Empty;          // e.g. "Neuro Objective Findings v1"
    public string SchemaJson { get; set; } = string.Empty;    // field defs, same idea as PatientFormSchema
    public bool IsActive { get; set; } = true;
}
