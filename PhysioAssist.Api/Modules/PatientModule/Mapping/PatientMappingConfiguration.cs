using Mapster;
using PhysioAssist.Api.Modules.PatientModule.DTOs;
using PhysioAssist.Api.Modules.PatientModule.Entities;

namespace PhysioAssist.Api.Modules.PatientModule.Mapping;

public class PatientMappingConfiguration : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<PatientRequest, Patient>();
        config.NewConfig<Patient, PatientResponse>();
    }
}