using System.Reflection;

namespace PhysioAssist.Api.Shared.Consts;

public static class Permissions
{
    public static string Type { get; } = "Permissions";

    public const string GetUsers = "User:Read";
    public const string CreateUsers = "User:create";   
    public const string UpdateUsers = "User:Update";

    public const string GetRoles = "Roles:Read";
    public const string CreateRoles = "Roles:create";
    public const string UpdateRoles = "Roles:Update";

    public const string Results = "results:Read";
    public const string ReadDashboard = "Dashboard:Read";

    public const string GetReceptionist = "receptionist:read";
    public const string CreateReceptionist = "receptionist:Write";


    
    public const string IntakeConvert = "Intake:Convert";

    public const string ReadInitialReport = "InitialReport:Read";
    public const string WriteInitialReport = "InitialReport:Write";

    public const string DailySessions = "Session:ViewToday";

    public const string ReadSession = "Session:Read";
    public const string WriteSession = "Session:Write";

    public const string WriteQueryAgent = "QueryAgent:Write";

    [PermissionMetadata("Manage schedule", "Create, edit, reschedule and cancel appointments.")]
    public const string ManageSchedule = "Schedule:Write";

    [PermissionMetadata("Read schedule", "View scheduled appointments.")]
    public const string ReadSchedule = "Schedule:Read";

    [PermissionMetadata("Read doctor availability", "View doctor working hours.")]
    public const string ReadWorkingSchedule = "WorkingSchedule:Read";

    [PermissionMetadata("Write doctor availability", "Set doctor working hours.")]
    public const string WriteWorkingSchedule = "WorkingSchedule:Write";

    [PermissionMetadata("Read patient data", "View patient information.")]
    public const string GetPatients = "Patient:Read";

    [PermissionMetadata("Write patient data", "Create and update patient information.")]
    public const string WritePatient = "Patient:Write";

    [PermissionMetadata("Read Form Data", "View forms with their rules and constraints.")]
    public const string IntakeRead = "Intake:Read";

    [PermissionMetadata("Manage patient forms", "Create and edit clinic intake forms for the patient portal.")]
    public const string IntakeManageForms = "Intake:ManageForms";

    [PermissionMetadata("Read submission data", "View submitted pateints and patients at receptioniest")]
    public const string SubmissionRead = "Submission:Read";

    [PermissionMetadata("generate QR code", "generate QR code for each form ")]
    public const string QRGenerate = "QR:Generate";
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

        /// <summary>
        /// Returns solo doctor permissions defined in this class.
        /// Use this to seed a role that should have solo doctor access.
        /// </summary>
        public static IReadOnlyList<string> GetSoloDoctorPermissions() => new[]
        {
            GetReceptionist,
            CreateReceptionist,

            IntakeRead,
            SubmissionRead,
            IntakeConvert,
            IntakeManageForms,

            ReadInitialReport,
            WriteInitialReport,

            DailySessions,
            ReadSession,
            WriteSession,

            QRGenerate,
            WriteQueryAgent,

            ManageSchedule,
            ReadSchedule,
            ReadWorkingSchedule,
            WriteWorkingSchedule,

            GetPatients,
            WritePatient,

            ReadDashboard
        };
}
