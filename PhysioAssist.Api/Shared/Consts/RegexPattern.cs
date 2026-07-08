namespace PhysioAssist.Api.Shared.Consts;

public class RegexPattern
{
    public const string Password = "(?=(.*[0-9]))(?=.*[\\!@#$%^&*()\\\\[\\]{}\\-_+=~`|:;\"'<>,./?])(?=.*[a-z])(?=(.*[A-Z]))(?=(.*)).{8,}";
    public const string UserName = "^[a-zA-Z][a-zA-Z0-9_]{2,15}$";
    public const string PhoneNumber = @"^(01)(0|1|2|5)\d{8}$";
}
