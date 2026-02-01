using Microsoft.Extensions.Configuration;
using Resend;


public class EmailService : IEmailService
{
    private readonly string _fromEmail;
    private readonly IResend _resend;

    public EmailService(IResend resend, IConfiguration configuration)
    {
        _resend = resend;
        _fromEmail = configuration["RESEND_FROM"] ?? "noreply@triptogether.com";
    }

    private async Task SendEmailAsync(string to, string subject, string htmlContent)
    {
        var message = new EmailMessage
        {
            From = _fromEmail,
            Subject = subject,
            HtmlBody = htmlContent
        };

        message.To.Add(to);
        await _resend.EmailSendAsync(message);
    }

    public async Task SendRegistrationSuccessEmailAsync(EmailRequestDto request)
    {
        var html = $@"
<html style=""background-color:#EDE8E0;margin:0;padding:0;"">
  <body style=""font-family:'IBM Plex Mono',-apple-system,BlinkMacSystemFont,'Courier New',monospace;color:#3D3229;padding:40px 20px;background-color:#EDE8E0;line-height:1.6;"">
    <div style=""max-width:560px;margin:auto;background:#FAF8F4;border:2px solid #996633;padding:32px;box-shadow:4px 4px 0 rgba(153,102,51,0.15);"">

      <!-- Header -->
      <div style=""text-align:center;margin-bottom:28px;padding-bottom:20px;border-bottom:2px solid #EDE8E0;"">
        <h1 style=""color:#996633;font-size:22px;font-weight:bold;margin:0;letter-spacing:2px;font-family:'IBM Plex Mono',monospace;"">TRIPTOGETHER</h1>
        <p style=""color:#7A6B5A;font-size:11px;margin:6px 0 0 0;letter-spacing:1px;"">GROUP TRAVEL MADE SIMPLE</p>
      </div>

      <!-- Welcome Message -->
      <div style=""text-align:center;margin-bottom:28px;"">
        <h2 style=""color:#3D3229;font-size:20px;font-weight:bold;margin:0 0 12px 0;"">Welcome, {request.UserName}!</h2>
        <p style=""color:#7A6B5A;font-size:13px;margin:0;line-height:1.7;"">You have successfully joined TripTogether. Start planning amazing trips with your friends and family.</p>
      </div>

      <!-- Feature Cards -->
      <div style=""margin:24px 0;"">
        <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse:collapse;"">
          <tr>
            <td style=""padding:12px;background:#EDE8E0;border:1px solid #D4C4B0;vertical-align:top;width:50%;"">
              <p style=""color:#996633;font-size:12px;font-weight:bold;margin:0 0 4px 0;letter-spacing:1px;"">PLAN TOGETHER</p>
              <p style=""color:#7A6B5A;font-size:11px;margin:0;line-height:1.5;"">Vote on dates, destinations, and budget as a group</p>
            </td>
            <td style=""padding:12px;background:#EDE8E0;border:1px solid #D4C4B0;border-left:none;vertical-align:top;width:50%;"">
              <p style=""color:#3D7A8C;font-size:12px;font-weight:bold;margin:0 0 4px 0;letter-spacing:1px;"">SHARE MEMORIES</p>
              <p style=""color:#7A6B5A;font-size:11px;margin:0;line-height:1.5;"">Upload photos and create a shared gallery</p>
            </td>
          </tr>
          <tr>
            <td style=""padding:12px;background:#EDE8E0;border:1px solid #D4C4B0;border-top:none;vertical-align:top;"">
              <p style=""color:#6B9B5C;font-size:12px;font-weight:bold;margin:0 0 4px 0;letter-spacing:1px;"">SMART LEDGER</p>
              <p style=""color:#7A6B5A;font-size:11px;margin:0;line-height:1.5;"">Track expenses and settle debts easily</p>
            </td>
            <td style=""padding:12px;background:#EDE8E0;border:1px solid #D4C4B0;border-left:none;border-top:none;vertical-align:top;"">
              <p style=""color:#996633;font-size:12px;font-weight:bold;margin:0 0 4px 0;letter-spacing:1px;"">COLLECT IDEAS</p>
              <p style=""color:#7A6B5A;font-size:11px;margin:0;line-height:1.5;"">Save places and activities to explore</p>
            </td>
          </tr>
        </table>
      </div>

      <!-- CTA Button -->
      <div style=""text-align:center;margin:32px 0;"">
        <a href=""https://triptogether.ae-tao-fullstack-api.site"" style=""display:inline-block;background:#996633;color:#FAF8F4;padding:14px 36px;text-decoration:none;font-weight:bold;font-size:12px;letter-spacing:2px;box-shadow:3px 3px 0 rgba(153,102,51,0.25);font-family:'IBM Plex Mono',monospace;"">START PLANNING</a>
      </div>

      <!-- Footer -->
      <div style=""border-top:2px solid #EDE8E0;padding-top:20px;margin-top:28px;text-align:center;"">
        <p style=""color:#7A6B5A;font-size:10px;margin:0;letter-spacing:1px;"">Happy Travels,</p>
        <p style=""color:#996633;font-size:11px;font-weight:bold;margin:4px 0 0 0;letter-spacing:1px;"">THE TRIPTOGETHER TEAM</p>
      </div>

    </div>
  </body>
</html>";
        await SendEmailAsync(request.To, "Welcome to TripTogether!", html);
    }

    public async Task SendOtpVerificationEmailAsync(EmailRequestDto request)
    {
        var html = $@"
<html style=""background-color:#EDE8E0;margin:0;padding:0;"">
  <body style=""font-family:'IBM Plex Mono',-apple-system,BlinkMacSystemFont,'Courier New',monospace;color:#3D3229;padding:40px 20px;background-color:#EDE8E0;line-height:1.6;"">
    <div style=""max-width:560px;margin:auto;background:#FAF8F4;border:2px solid #996633;padding:32px;box-shadow:4px 4px 0 rgba(153,102,51,0.15);"">

      <!-- Header -->
      <div style=""text-align:center;margin-bottom:28px;padding-bottom:20px;border-bottom:2px solid #EDE8E0;"">
        <h1 style=""color:#996633;font-size:22px;font-weight:bold;margin:0;letter-spacing:2px;font-family:'IBM Plex Mono',monospace;"">TRIPTOGETHER</h1>
        <p style=""color:#7A6B5A;font-size:11px;margin:6px 0 0 0;letter-spacing:1px;"">EMAIL VERIFICATION</p>
      </div>

      <!-- Message -->
      <div style=""text-align:center;margin-bottom:24px;"">
        <p style=""color:#3D3229;font-size:14px;margin:0 0 8px 0;font-weight:bold;"">Verify Your Email Address</p>
        <p style=""color:#7A6B5A;font-size:12px;margin:0;line-height:1.6;"">Enter the code below to complete your registration</p>
      </div>

      <!-- OTP Code Box -->
      <div style=""text-align:center;margin:28px 0;"">
        <div style=""display:inline-block;background:#3D3229;padding:20px 32px;box-shadow:4px 4px 0 rgba(61,50,41,0.2);"">
          <div style=""color:#FAF8F4;font-size:32px;font-weight:bold;letter-spacing:12px;font-family:'IBM Plex Mono',monospace;"">{request.Otp}</div>
        </div>
      </div>

      <!-- Timer Notice -->
      <div style=""text-align:center;margin:24px 0;"">
        <div style=""display:inline-block;background:#EDE8E0;padding:10px 20px;border:1px solid #D4C4B0;"">
          <p style=""color:#7A6B5A;font-size:11px;margin:0;font-family:'IBM Plex Mono',monospace;"">CODE EXPIRES IN <span style=""color:#996633;font-weight:bold;"">10 MINUTES</span></p>
        </div>
      </div>

      <!-- Help Text -->
      <p style=""color:#7A6B5A;font-size:11px;margin:20px 0 0 0;text-align:center;line-height:1.6;"">If you did not request this code, you can safely ignore this email.</p>

      <!-- Footer -->
      <div style=""border-top:2px solid #EDE8E0;padding-top:20px;margin-top:28px;text-align:center;"">
        <p style=""color:#7A6B5A;font-size:10px;margin:0;letter-spacing:1px;"">Happy Travels,</p>
        <p style=""color:#996633;font-size:11px;font-weight:bold;margin:4px 0 0 0;letter-spacing:1px;"">THE TRIPTOGETHER TEAM</p>
      </div>

    </div>
  </body>
</html>";
        await SendEmailAsync(request.To, "Verify Your Email - TripTogether", html);
    }

    public async Task SendForgotPasswordOtpEmailAsync(EmailRequestDto request)
    {
        var html = $@"
<html style=""background-color:#EDE8E0;margin:0;padding:0;"">
  <body style=""font-family:'IBM Plex Mono',-apple-system,BlinkMacSystemFont,'Courier New',monospace;color:#3D3229;padding:40px 20px;background-color:#EDE8E0;line-height:1.6;"">
    <div style=""max-width:560px;margin:auto;background:#FAF8F4;border:2px solid #996633;padding:32px;box-shadow:4px 4px 0 rgba(153,102,51,0.15);"">

      <!-- Header -->
      <div style=""text-align:center;margin-bottom:28px;padding-bottom:20px;border-bottom:2px solid #EDE8E0;"">
        <h1 style=""color:#996633;font-size:22px;font-weight:bold;margin:0;letter-spacing:2px;font-family:'IBM Plex Mono',monospace;"">TRIPTOGETHER</h1>
        <p style=""color:#7A6B5A;font-size:11px;margin:6px 0 0 0;letter-spacing:1px;"">PASSWORD RESET</p>
      </div>

      <!-- Message -->
      <div style=""text-align:center;margin-bottom:24px;"">
        <p style=""color:#3D3229;font-size:14px;margin:0 0 8px 0;font-weight:bold;"">Reset Your Password</p>
        <p style=""color:#7A6B5A;font-size:12px;margin:0;line-height:1.6;"">Use the code below to reset your password</p>
      </div>

      
      <div style=""text-align:center;margin:28px 0;"">
        <div style=""display:inline-block;background:#3D3229;padding:20px 32px;box-shadow:4px 4px 0 rgba(61,50,41,0.2);"">
          <div style=""color:#FAF8F4;font-size:32px;font-weight:bold;letter-spacing:12px;font-family:'IBM Plex Mono',monospace;"">{request.Otp}</div>
        </div>
      </div>

      <!-- Timer Notice -->
      <div style=""text-align:center;margin:24px 0;"">
        <div style=""display:inline-block;background:#EDE8E0;padding:10px 20px;border:1px solid #D4C4B0;"">
          <p style=""color:#7A6B5A;font-size:11px;margin:0;font-family:'IBM Plex Mono',monospace;"">CODE EXPIRES IN <span style=""color:#996633;font-weight:bold;"">15 MINUTES</span></p>
        </div>
      </div>

      <!-- Security Warning -->
      <div style=""background:#3D3229;padding:14px 16px;margin:20px 0;"">
        <p style=""color:#FAF8F4;font-size:11px;margin:0;text-align:center;font-family:'IBM Plex Mono',monospace;letter-spacing:0.5px;"">SECURITY: Never share this code with anyone</p>
      </div>

      <!-- Help Text -->
      <p style=""color:#7A6B5A;font-size:11px;margin:16px 0 0 0;text-align:center;line-height:1.6;"">If you did not request this reset, you can safely ignore this email.</p>

      <!-- Footer -->
      <div style=""border-top:2px solid #EDE8E0;padding-top:20px;margin-top:28px;text-align:center;"">
        <p style=""color:#7A6B5A;font-size:10px;margin:0;letter-spacing:1px;"">Happy Travels,</p>
        <p style=""color:#996633;font-size:11px;font-weight:bold;margin:4px 0 0 0;letter-spacing:1px;"">THE TRIPTOGETHER TEAM</p>
      </div>

    </div>
  </body>
</html>";
        await SendEmailAsync(request.To, "Password Reset - TripTogether", html);
    }

    public async Task SendPasswordChangeSuccessAsync(EmailRequestDto request)
    {
        var html = $@"
<html style=""background-color:#EDE8E0;margin:0;padding:0;"">
  <body style=""font-family:'IBM Plex Mono',-apple-system,BlinkMacSystemFont,'Courier New',monospace;color:#3D3229;padding:40px 20px;background-color:#EDE8E0;line-height:1.6;"">
    <div style=""max-width:560px;margin:auto;background:#FAF8F4;border:2px solid #996633;padding:32px;box-shadow:4px 4px 0 rgba(153,102,51,0.15);"">

      <!-- Header -->
      <div style=""text-align:center;margin-bottom:28px;padding-bottom:20px;border-bottom:2px solid #EDE8E0;"">
        <h1 style=""color:#996633;font-size:22px;font-weight:bold;margin:0;letter-spacing:2px;font-family:'IBM Plex Mono',monospace;"">TRIPTOGETHER</h1>
        <p style=""color:#7A6B5A;font-size:11px;margin:6px 0 0 0;letter-spacing:1px;"">SECURITY UPDATE</p>
      </div>

      <!-- Success Icon & Message -->
      <div style=""text-align:center;margin-bottom:24px;"">
        <div style=""display:inline-block;background:#6B9B5C;width:48px;height:48px;line-height:48px;margin-bottom:16px;"">
          <span style=""font-size:24px;color:#FAF8F4;"">&#10003;</span>
        </div>
        <p style=""color:#6B9B5C;font-size:14px;margin:0 0 8px 0;font-weight:bold;letter-spacing:1px;"">PASSWORD CHANGED</p>
      </div>

      <!-- Message -->
      <div style=""text-align:center;margin-bottom:24px;"">
        <p style=""color:#3D3229;font-size:14px;margin:0 0 12px 0;"">Hello <strong>{request.UserName}</strong>,</p>
        <p style=""color:#7A6B5A;font-size:12px;margin:0;line-height:1.7;"">Your password has been successfully updated. You can now log in with your new password.</p>
      </div>

      <!-- CTA Button -->
      <div style=""text-align:center;margin:28px 0;"">
        <a href=""https://triptogether.ae-tao-fullstack-api.site/login"" style=""display:inline-block;background:#996633;color:#FAF8F4;padding:14px 36px;text-decoration:none;font-weight:bold;font-size:12px;letter-spacing:2px;box-shadow:3px 3px 0 rgba(153,102,51,0.25);font-family:'IBM Plex Mono',monospace;"">LOGIN NOW</a>
      </div>

      <!-- Security Warning -->
      <div style=""background:#3D3229;padding:14px 16px;margin:20px 0;"">
        <p style=""color:#FAF8F4;font-size:11px;margin:0;text-align:center;font-family:'IBM Plex Mono',monospace;letter-spacing:0.5px;"">If you did not make this change, contact support immediately</p>
      </div>

      <!-- Footer -->
      <div style=""border-top:2px solid #EDE8E0;padding-top:20px;margin-top:28px;text-align:center;"">
        <p style=""color:#7A6B5A;font-size:10px;margin:0;letter-spacing:1px;"">Happy Travels,</p>
        <p style=""color:#996633;font-size:11px;font-weight:bold;margin:4px 0 0 0;letter-spacing:1px;"">THE TRIPTOGETHER TEAM</p>
      </div>

    </div>
  </body>
</html>";
        await SendEmailAsync(request.To, "Password Changed Successfully - TripTogether", html);
    }

}
