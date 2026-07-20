using PhysioAssist.Api.Modules.Notification.DTO;
using System.Net;

namespace PhysioAssist.Api.Modules.Notification.Templates
{
    /// <summary>
    /// Single shared HTML shell for every appointment email. Individual templates
    /// in EmailTemplateService only supply the heading/message/accent color —
    /// this class owns the actual markup, so the clinic's visual identity lives
    /// in exactly one place and every email stays visually consistent.
    /// </summary>
    internal static class EmailLayout
    {
        public static string Wrap(
            string heading,
            string greeting,
            string message,
            AppointmentNotificationDto appointment,
            string accentColor)
        {
            // WebUtility.HtmlEncode guards against a patient/doctor display name
            // that happens to contain HTML-special characters breaking the markup
            // or enabling injection into the rendered email.
            var safeGreeting = WebUtility.HtmlEncode(greeting);
            var safeMessage = WebUtility.HtmlEncode(message);
            var safeDoctorName = WebUtility.HtmlEncode(appointment.DoctorName);

            var dateLabel = appointment.SlotStart.ToString("d MMMM yyyy");
            var timeLabel = $"{appointment.SlotStart:h:mm tt} – {appointment.SlotEnd:h:mm tt}";

            return $$"""
        <!DOCTYPE html>
        <html>
        <body style="margin:0;padding:0;background-color:#F8FAFC;font-family:Segoe UI, Arial, sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="padding:32px 0;">
            <tr>
              <td align="center">
                <table width="480" cellpadding="0" cellspacing="0" style="background:#FFFFFF;border-radius:12px;overflow:hidden;box-shadow:0 1px 3px rgba(15,23,42,0.08);">
                  <tr>
                    <td style="background:{{accentColor}};padding:20px 32px;">
                      <span style="color:#FFFFFF;font-size:18px;font-weight:700;">PhysioAssist</span>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:32px;">
                      <h1 style="margin:0 0 16px;font-size:20px;color:#0F172A;">{{heading}}</h1>
                      <p style="margin:0 0 8px;font-size:14px;color:#0F172A;">{{safeGreeting}}</p>
                      <p style="margin:0 0 24px;font-size:14px;color:#475569;line-height:1.5;">{{safeMessage}}</p>

                      <table width="100%" cellpadding="0" cellspacing="0" style="border:1px solid #E2E8F0;border-radius:8px;overflow:hidden;">
                        <tr>
                          <td style="padding:12px 16px;font-size:12px;color:#475569;border-bottom:1px solid #E2E8F0;">Doctor</td>
                          <td style="padding:12px 16px;font-size:13px;color:#0F172A;font-weight:600;text-align:right;border-bottom:1px solid #E2E8F0;">{{safeDoctorName}}</td>
                        </tr>
                        <tr>
                          <td style="padding:12px 16px;font-size:12px;color:#475569;border-bottom:1px solid #E2E8F0;">Date</td>
                          <td style="padding:12px 16px;font-size:13px;color:#0F172A;font-weight:600;text-align:right;border-bottom:1px solid #E2E8F0;">{{dateLabel}}</td>
                        </tr>
                        <tr>
                          <td style="padding:12px 16px;font-size:12px;color:#475569;">Time</td>
                          <td style="padding:12px 16px;font-size:13px;color:#0F172A;font-weight:600;text-align:right;">{{timeLabel}}</td>
                        </tr>
                      </table>

                      <p style="margin:24px 0 0;font-size:13px;color:#94A3B8;">Thank you,<br/>PhysioAssist Clinic</p>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;
        }
    }
}
