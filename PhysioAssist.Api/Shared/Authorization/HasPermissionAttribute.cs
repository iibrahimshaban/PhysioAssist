using Microsoft.AspNetCore.Authorization;

namespace PhysioAssist.Api.Shared.Authorization;

public class HasPermissionAttribute(string permission) : AuthorizeAttribute(permission)
{
}

