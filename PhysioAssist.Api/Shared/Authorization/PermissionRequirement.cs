using Microsoft.AspNetCore.Authorization;

namespace PhysioAssist.Api.Shared.Authorization;

public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}