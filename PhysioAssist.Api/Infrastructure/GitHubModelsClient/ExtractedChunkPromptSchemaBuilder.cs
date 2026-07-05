using PhysioAssist.Api.Shared.Dtos.Chunking;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace PhysioAssist.Api.Infrastructure.GitHubModelsClient;

public static class ExtractedChunkPromptSchemaBuilder
{
    public static string BuildFieldDescriptions()
    {
        var sb = new StringBuilder();
        var properties = typeof(ExtractedChunk).GetProperties();

        foreach (var prop in properties)
        {
            var description = prop.GetCustomAttribute<DescriptionAttribute>()?.Description
                ?? "(no description provided)";
            var isNullable = Nullable.GetUnderlyingType(prop.PropertyType) is not null
                || (prop.PropertyType == typeof(string) && IsNullableReferenceType(prop));

            sb.AppendLine($"  \"{prop.Name}\": \"{description}\"{(isNullable ? " (nullable)" : "")}");
        }

        return sb.ToString();
    }

    // Reflection can't see nullable reference type annotations directly without NullabilityInfoContext
    private static bool IsNullableReferenceType(PropertyInfo prop)
    {
        var context = new NullabilityInfoContext();
        return context.Create(prop).WriteState == NullabilityState.Nullable;
    }
}
