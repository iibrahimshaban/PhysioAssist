using PhysioAssist.Api.Modules.Notification.DTO;
using PhysioAssist.Api.Modules.Notification.Interfaces;
using PhysioAssist.Api.Modules.Notification.Templates;

namespace PhysioAssist.Api.Modules.Notification.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        public (string Subject, string HtmlBody) BuildAppointmentCreated(AppointmentNotificationDto appointment)
        {
            var body = EmailLayout.Wrap(
                heading: "Appointment Confirmed",
                greeting: $"Hello {appointment.PatientName},",
                message: "Your appointment has been confirmed. Here are the details:",
                appointment: appointment,
                accentColor: "#2563EB");

            return ("Appointment Confirmed", body);
        }

        public (string Subject, string HtmlBody) BuildAppointmentCancelled(AppointmentNotificationDto appointment)
        {
            var body = EmailLayout.Wrap(
                heading: "Appointment Cancelled",
                greeting: $"Hello {appointment.PatientName},",
                message: "Your appointment has been cancelled. If this was unexpected, please contact the clinic.",
                appointment: appointment,
                accentColor: "#EF4444");

            return ("Appointment Cancelled", body);
        }

        public (string Subject, string HtmlBody) BuildAppointmentRescheduled(AppointmentNotificationDto appointment)
        {
            var body = EmailLayout.Wrap(
                heading: "Appointment Rescheduled",
                greeting: $"Hello {appointment.PatientName},",
                message: "Your appointment has been rescheduled. The new details are below:",
                appointment: appointment,
                accentColor: "#F59E0B");

            return ("Appointment Rescheduled", body);
        }

        public (string Subject, string HtmlBody) BuildAppointmentCompleted(AppointmentNotificationDto appointment)
        {
            var body = EmailLayout.Wrap(
                heading: "Appointment Completed",
                greeting: $"Hello {appointment.PatientName},",
                message: "Thank you for attending your appointment. We hope your session went well.",
                appointment: appointment,
                accentColor: "#22C55E");

            return ("Appointment Completed", body);
        }

        public (string Subject, string HtmlBody) BuildAppointmentNoShow(AppointmentNotificationDto appointment)
        {
            var body = EmailLayout.Wrap(
                heading: "Missed Appointment",
                greeting: $"Hello {appointment.PatientName},",
                message: "We noticed you were unable to attend your scheduled appointment. Please contact the clinic to reschedule.",
                appointment: appointment,
                accentColor: "#F59E0B");

            return ("Missed Appointment", body);
        }
    }
}
