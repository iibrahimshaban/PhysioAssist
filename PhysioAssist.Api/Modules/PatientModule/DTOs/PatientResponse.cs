namespace PhysioAssist.Api.Modules.PatientModule.DTOs
{
    public class PatientResponse
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } 
        public string PhoneNumber { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string QRCodeToken { get; set; } = string.Empty;
        public PatientStatus Status { get; set; } 

    }
}
