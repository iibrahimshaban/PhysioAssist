namespace PhysioAssist.Api.Shared.Consts;

public class RegexPattern
{
    public const string Password = "(?=(.*[0-9]))(?=.*[\\!@#$%^&*()\\\\[\\]{}\\-_+=~`|:;\"'<>,./?])(?=.*[a-z])(?=(.*[A-Z]))(?=(.*)).{8,}";
    public const string UserName = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
    public const string PhoneNumber = @"^(?:\+20|0)1[0125]\d{8}$";
}
