using Mapster;
using PhysioAssist.Api.Modules.Auth.Contracts.Authentication;
using PhysioAssist.Api.Modules.Auth.Contracts.User;
using PhysioAssist.Api.Modules.Auth.Entities;

namespace PhysioAssist.Api.Modules.Auth.Mapping;

public class MappingConfiguration() : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<(ApplicationUser user, IList<string> roles), UserResponse>()
            .Map(dest => dest.Roles, src => src.roles)
            .Map(dest => dest, src => src.user);

        config.NewConfig<ApplicationUser, AuthResponse>()
            .Map(dest => dest.ProfilePictureUrl, src => src.ProfilePictureUrl);
    }
}
