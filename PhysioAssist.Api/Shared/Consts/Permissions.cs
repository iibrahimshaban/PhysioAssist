using System.Reflection;

namespace PhysioAssist.Api.Shared.Consts;

public static class Permissions
{
    public static string Type { get; } = "Permissions";

    public const string GetUsers = "User:Read";
    public const string CreateUsers = "User:create";   
    public const string UpdateUsers = "User:Update";

    public const string GetReceptionist = "receptionist:read";
    public const string CreateReceptionist = "receptionist:create";
    public const string UpdateReceptionist = "receptionist:Update";

    public const string GetRoles = "Roles:Read";
    public const string CreateRoles = "Roles:create";
    public const string UpdateRoles = "Roles:Update";

    public const string Results = "results:Read";

    public const string IntakeRead = "Intake:Read";
    public const string IntakeManageForms = "Intake:ManageForms";
    public const string IntakeReview = "Intake:Review";
    public const string IntakeConvert = "Intake:Convert";

    public const string QRGenerate = "QR:Generate";
    public const string QRValidate = "QR:Validate";

    [PermissionMetadata("Manage schedule", "Create, edit and cancel appointments.")]
    public const string ManageSchedule = "receptionist:manage-schedule";

    [PermissionMetadata("Check in patients", "Mark arrivals and update queue.")]
    public const string CheckInPatients = "receptionist:check-in-patients";

    [PermissionMetadata("Create session package", "Create a new patient treatment session package.")]
    public const string CreateSessionPackage = "scheduling:create-session-package";

    [PermissionMetadata("View session candidates", "View suggested slots for a patient's next session.")]
    public const string GetSessionCandidates = "scheduling:get-session-candidates";

    [PermissionMetadata("Confirm session slot", "Book a specific session slot for a patient.")]
    public const string ConfirmSessionSlot = "scheduling:confirm-session-slot";

    public static IList<string?> GetAllPermissions() =>
        [.. typeof(Permissions).GetFields().Select(field => field.GetValue(field) as string)];

    private static readonly Lazy<Dictionary<string, PermissionInfo>> _metadata = new(BuildMetadata);
    public static IReadOnlyDictionary<string, PermissionInfo> Metadata => _metadata.Value;

    private static Dictionary<string, PermissionInfo> BuildMetadata() =>
        typeof(Permissions)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string) && f.IsLiteral)
            .Select(f => new
            {
                Value = (string)f.GetRawConstantValue()!,
                Attr = f.GetCustomAttribute<PermissionMetadataAttribute>()
            })
            .Where(x => x.Attr is not null)
            .ToDictionary(
                x => x.Value,
                x => new PermissionInfo(x.Value, x.Attr!.Title, x.Attr.Description));
}
