using Mapster;
using PhysioAssist.Api.Modules.Intake.DTOs.FormSchemas;
using PhysioAssist.Api.Modules.Intake.DTOs.PublicAccess;
using PhysioAssist.Api.Modules.Intake.DTOs.Submissions;
using PhysioAssist.Api.Modules.Intake.Entities;

namespace PhysioAssist.Api.Modules.Intake.Mapping;

public class IntakeMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<PatientFormSchema, FormSchemaResponse>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.SchemaJson, src => src.SchemaJson)
            .Map(dest => dest.DoctorId, src => src.DoctorId)
            .Map(dest => dest.Version, src => src.Version)
            .Map(dest => dest.Status, src => src.Status)
            .Map(dest => dest.IsDefault, src => src.IsDefault)
            .Map(dest => dest.SchemaHash, src => src.SchemaHash)
            .Map(dest => dest.PublishedAt, src => src.PublishedAt)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt);

        config.NewConfig<PatientFormSchema, FormSchemaSummaryResponse>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.Version, src => src.Version)
            .Map(dest => dest.Status, src => src.Status)
            .Map(dest => dest.IsDefault, src => src.IsDefault)
            .Map(dest => dest.PublishedAt, src => src.PublishedAt)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);

        config.NewConfig<PreVisitIntake, PreVisitIntakeResponse>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.DoctorId, src => src.DoctorId)
            .Map(dest => dest.FormSchemaId, src => src.FormSchemaId)
            .Map(dest => dest.FormSchemaVersion, src => src.FormSchemaVersion)
            .Map(dest => dest.PatientName, src => src.PatientName)
            .Map(dest => dest.PatientEmail, src => src.PatientEmail)
            .Map(dest => dest.PatientPhone, src => src.PatientPhone)
            .Map(dest => dest.Status, src => src.Status)
            .Map(dest => dest.ConvertedToPatientId, src => src.ConvertedToPatientId)
            .Map(dest => dest.SubmittedAt, src => src.SubmittedAt)
            .Map(dest => dest.ReviewedAt, src => src.ReviewedAt)
            .Map(dest => dest.ReviewedByDoctorId, src => src.ReviewedByDoctorId);

        config.NewConfig<PreVisitIntake, PreVisitIntakeDetailsResponse>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.DoctorId, src => src.DoctorId)
            .Map(dest => dest.FormSchemaId, src => src.FormSchemaId)
            .Map(dest => dest.FormSchemaVersion, src => src.FormSchemaVersion)
            .Map(dest => dest.PatientName, src => src.PatientName)
            .Map(dest => dest.PatientEmail, src => src.PatientEmail)
            .Map(dest => dest.PatientPhone, src => src.PatientPhone)
            .Map(dest => dest.FormSubmissionData, src => src.FormSubmissionData)
            .Map(dest => dest.PainPointsData, src => src.PainPointsData)
            .Map(dest => dest.Status, src => src.Status)
            .Map(dest => dest.ConvertedToPatientId, src => src.ConvertedToPatientId)
            .Map(dest => dest.SubmittedAt, src => src.SubmittedAt)
            .Map(dest => dest.ReviewedAt, src => src.ReviewedAt)
            .Map(dest => dest.ReviewedByDoctorId, src => src.ReviewedByDoctorId)
            .Map(dest => dest.FormSchemaName, src => src.FormSchema != null ? src.FormSchema.Name : string.Empty);

        config.NewConfig<CreateFormSchemaRequest, PatientFormSchema>()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.SchemaJson, src => src.SchemaJson)
            .Map(dest => dest.IsDefault, src => src.IsDefault);

        config.NewConfig<UpdateFormSchemaRequest, PatientFormSchema>()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.SchemaJson, src => src.SchemaJson)
            .Map(dest => dest.IsDefault, src => src.IsDefault);

        config.NewConfig<SubmitPreVisitIntakeRequest, PreVisitIntake>()
            .Map(dest => dest.PatientName, src => src.PatientName)
            .Map(dest => dest.PatientEmail, src => src.PatientEmail)
            .Map(dest => dest.PatientPhone, src => src.PatientPhone)
            .Map(dest => dest.FormSubmissionData, src => src.FormSubmissionData)
            .Map(dest => dest.PainPointsData, src => src.PainPointsData);

        // Public Access Mappings
        config.NewConfig<PatientFormSchema, PublicIntakeFormResponse>()
            .Map(dest => dest.FormSchemaId, src => src.Id)
            .Map(dest => dest.FormName, src => src.Name)
            .Map(dest => dest.FormDescription, src => src.Description)
            .Map(dest => dest.SchemaJson, src => src.SchemaJson)
            .Map(dest => dest.Version, src => src.Version);

        config.NewConfig<PreVisitIntake, PublicIntakeSubmissionResponse>()
            .Map(dest => dest.SubmissionId, src => src.Id)
            .Map(dest => dest.SubmittedAt, src => src.SubmittedAt)
            .Ignore(dest => dest.Message);
    }
}
