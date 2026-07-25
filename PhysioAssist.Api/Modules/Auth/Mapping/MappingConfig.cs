using Mapster;
using PhysioAssist.Api.Modules.Auth.Contracts.Authentication;
using PhysioAssist.Api.Modules.Auth.Contracts.User;
using PhysioAssist.Api.Modules.Auth.Entities;
using PhysioAssist.Api.Shared.Dtos.Doctor;

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

        config.NewConfig<Doctor, DoctorResponse>()
            .Map(dest => dest.FirstName, src => src.User.FirstName)
            .Map(dest => dest.LastName, src => src.User.LastName);

    }
}
