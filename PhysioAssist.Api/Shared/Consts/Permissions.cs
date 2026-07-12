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

    public const string IntakeRead = "Intake:Read";
    public const string IntakeManageForms = "Intake:ManageForms";
    public const string IntakeReview = "Intake:Review";
    public const string IntakeConvert = "Intake:Convert";

    public const string QRGenerate = "QR:Generate";
    public const string QRValidate = "QR:Validate";

    public static IList<string?> GetAllPermissions() =>
        [.. typeof(Permissions).GetFields().Select(field => field.GetValue(field) as string)];
}
