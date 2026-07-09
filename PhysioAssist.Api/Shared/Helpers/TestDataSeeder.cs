using Microsoft.AspNetCore.Identity;
using PhysioAssist.Api.Modules.Auth.Entities;
using PhysioAssist.Api.Modules.PatientModule.Entities;
using PhysioAssist.Api.Modules.SessionModule.Entities;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Consts;

namespace PhysioAssist.Api.Shared.Helpers;

public static class TestDataSeeder
{
    // Fixed GUIDs so you can reference them directly in Swagger/Postman without
    // hunting through the DB after seeding.
    public static readonly Guid TestDoctorId = Guid.Parse("019e3000-0000-7000-8000-000000000001");
    public static readonly Guid TestPatientId = Guid.Parse("019e3000-0000-7000-8000-000000000002");
    public static readonly Guid TestSession1Id = Guid.Parse("019e3000-0000-7000-8000-000000000011");
    public static readonly Guid TestSession2Id = Guid.Parse("019e3000-0000-7000-8000-000000000012");
    public static readonly Guid TestSession3Id = Guid.Parse("019e3000-0000-7000-8000-000000000013");

    public const string TestDoctorEmail = "test.doctor@physioassist.local";
    public const string TestDoctorPassword = "P@ssword123";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (await userManager.FindByIdAsync(TestDoctorId.ToString()) is not null)
            return; // idempotent — already seeded

        var user = new ApplicationUser
        {
            Id = TestDoctorId.ToString(),
            Email = TestDoctorEmail,
            UserName = TestDoctorEmail,
            FirstName = "Test",
            LastName = "Doctor",
            IsDisabled = false,
            EmailConfirmed = true, // skip OTP flow for test login
            ProfilePictureUrl = string.Empty
        };

        await userManager.CreateAsync(user, TestDoctorPassword);
        await userManager.AddToRoleAsync(user, DefaultRoles.SoloDoctor);

        context.Doctors.Add(new Doctor
        {
            Id = TestDoctorId, // same GUID as ApplicationUser.Id
            UserId = TestDoctorId.ToString(),
            ClinicName = "Test Neuro Rehab Clinic"
        });

        context.Patients.Add(new Patient
        {
            Id = TestPatientId,
            FullName = "Test Patient (Neuro)",
            DateOfBirth = new DateTime(1978, 3, 14, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            PhoneNumber = "01000000000",
            EmailAddress = "test.patient@physioassist.local",
            QRCodeToken = Guid.NewGuid().ToString("N"),
            Status = PatientStatus.Active,
            CreatedById = DefaultUsers.UserId,
        });

        context.DoctorPatients.Add(new DoctorPatient
        {
            DoctorId = TestDoctorId,
            PatientId = TestPatientId,
            IsPrimary = true,
            AssignedAt = DateTime.UtcNow,
            AccessLevel = AccessLevel.Owner,
            Status = DoctorPatientStatus.Active,
            Category = PatientCategory.Neurological
        });

        var sessions = new[]
        {
            new
            {
                Id = TestSession1Id,
                Transcript = """
                    Patient presents post-stroke, six weeks out, for initial neuro PT session.
                    Berg Balance Scale administered today, score 32 out of 56, indicating
                    moderate fall risk. Modified Ashworth Scale for right biceps is 2,
                    left biceps is 1. Manual muscle testing shows right hip flexors at 3
                    out of 5 strength, left hip flexors 4 out of 5. Sensation intact
                    bilaterally in lower extremities. Coordination testing shows mild
                    dysmetria on finger-to-nose on the right side. Transfers from sit to
                    stand require moderate assistance. Patient currently uses a front
                    wheeled walker for ambulation.
                    """
            },
            new
            {
                Id = TestSession2Id,
                Transcript = """
                    Second session, two weeks after initial evaluation. Berg Balance Scale
                    re-tested, improved to 38 out of 56. Modified Ashworth Scale right
                    biceps now 1+, showing reduced tone. Manual muscle testing right hip
                    flexors improved to 4 out of 5. Transfers now require only minimal
                    assistance, notable improvement from moderate assist. Patient still
                    ambulating with front wheeled walker but demonstrating better stability.
                    """
            },
            new
            {
                Id = TestSession3Id,
                Transcript = """
                    Third session, patient now ambulatory enough to switch to Functional
                    Gait Assessment instead of Berg. FGA score today is 18 out of 30.
                    Gait speed measured at 0.6 meters per second. Coordination on
                    finger-to-nose now within normal limits, no dysmetria noted. Transfers
                    are now independent with supervision only for safety. Patient has
                    progressed from the front wheeled walker to a single point cane.
                    """
            }
        };

        foreach (var s in sessions)
        {
            context.Sessions.Add(new Session
            {
                Id = s.Id,
                DoctorId = TestDoctorId,
                PatientId = TestPatientId,
                Status = SessionStatus.Completed,
                CreatedById = DefaultUsers.UserId,
                Transcription = new SessionTranscription
                {
                    Id = Guid.CreateVersion7(),
                    SessionId = s.Id,
                    RawTranscript = s.Transcript,
                    AudioFileUrl = string.Empty,
                    Language = AudioLanguage.Mixed,
                    DurationSeconds = 300,
                    Status = TranscriptionStatus.Completed,
                    CreatedById = DefaultUsers.UserId
                }
            });
        }

        await context.SaveChangesAsync();
    }
}

