namespace PhysioAssist.Api.Shared.Authorization;

[AttributeUsage(AttributeTargets.Field)]
public class PermissionMetadataAttribute(string title, string description) : Attribute
{
    public string Title { get; } = title;
    public string Description { get; } = description;
}

public record PermissionInfo(string Value, string Title, string Description);
