namespace PhysioAssist.Api.Shared.Helpers;

public static class EmailBodyBuilder
{
    private static string BaseTemplate(string title, string userName,
    string bodyMessage, string otpCode, string codeLabel,
    string accentColor, string accentBg, string footerNote)
    {
        return $"""
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="UTF-8"/><meta name="viewport" content="width=device-width,initial-scale=1"/></head>
        <body style="margin:0;padding:0;background:#f4f6fb;font-family:Arial,Helvetica,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f6fb;padding:32px 0;">
            <tr><td align="center">
              <table width="520" cellpadding="0" cellspacing="0"
                     style="background:#ffffff;border-radius:12px;overflow:hidden;border:1px solid #e5e7eb;">

                <!-- header -->
                <tr>
                  <td style="background:#0F766E;padding:24px;text-align:center;">
                    <span style="font-size:22px;font-weight:700;color:#ffffff;letter-spacing:-0.5px;">
                      Physio<span style="color:#99f6e4;">Assist</span>.
                    </span>
                  </td>
                </tr>

                <!-- body -->
                <tr>
                  <td style="padding:32px 36px 24px;">
                    <p style="font-size:15px;color:#111827;margin:0 0 8px;">
                      Hi <strong>{userName}</strong>,
                    </p>
                    <p style="font-size:13px;color:#6b7280;line-height:1.7;margin:0 0 28px;">
                      {bodyMessage}
                    </p>

                    <!-- OTP block -->
                    <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom:28px;">
                      <tr>
                        <td style="background:{accentBg};border-radius:10px;padding:20px;text-align:center;">
                          <p style="font-size:11px;color:{accentColor};margin:0 0 8px;
                                     letter-spacing:2px;text-transform:uppercase;">
                            {codeLabel}
                          </p>
                          <p style="font-size:32px;font-weight:700;letter-spacing:12px;
                                     color:{accentColor};margin:0;">
                            {otpCode}
                          </p>
                          <p style="font-size:11px;color:{accentColor};margin:10px 0 0;opacity:0.75;">
                            Expires in 10 minutes
                          </p>
                        </td>
                      </tr>
                    </table>

                    <p style="font-size:12px;color:#9ca3af;line-height:1.7;margin:0;">
                      {footerNote}
                    </p>
                  </td>
                </tr>

                <!-- footer -->
                <tr>
                  <td style="background:#f9fafb;border-top:1px solid #e5e7eb;
                              padding:14px 36px;text-align:center;">
                    <p style="font-size:11px;color:#9ca3af;margin:0;">
                      © 2025 PhysioAssist &bull; All rights reserved
                    </p>
                  </td>
                </tr>

              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;
    }

    public static string EmailConfirmation(string userName, string otpCode) =>
        BaseTemplate(
            title: "Verify your email",
            userName: userName,
            bodyMessage: "Thanks for signing up! Please use the code below to verify your " +
                         "email address and activate your PhysioAssist account.",
            otpCode: otpCode,
            codeLabel: "Verification code",
            accentColor: "#0F766E",
            accentBg: "#f0fdfa",
            footerNote: "If you didn't create a PhysioAssist account, you can safely ignore this email."
        );

    public static string PasswordReset(string userName, string otpCode) =>
        BaseTemplate(
            title: "Reset your password",
            userName: userName,
            bodyMessage: "We received a request to reset the password for your PhysioAssist account. " +
                         "Use the code below to proceed. If you didn't request this, ignore this email — " +
                         "your password will not be changed.",
            otpCode: otpCode,
            codeLabel: "Reset code",
            accentColor: "#ea580c",
            accentBg: "#fff7ed",
            footerNote: "For security, this code can only be used once and expires in 10 minutes."
        );
}