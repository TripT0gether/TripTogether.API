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
<html style=""background-color:#0a0e27;margin:0;padding:0;"">
  <body style=""font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;color:#ffffff;padding:40px 20px;background-color:#0a0e27;line-height:1.6;"">
    <div style=""max-width:600px;margin:auto;background:linear-gradient(135deg, #1a1f3a 0%, #2d3561 100%);border:1px solid #4a90e2;border-radius:16px;padding:40px;box-shadow:0 20px 40px rgba(74,144,226,0.15);"">

      <div style=""text-align:center;margin-bottom:32px;"">
        <div style=""display:inline-block;background:linear-gradient(135deg, #4a90e2 0%, #357abd 100%);padding:16px 24px;border-radius:12px;box-shadow:0 8px 20px rgba(74,144,226,0.3);"">
          <h2 style=""color:#ffffff;font-size:28px;font-weight:bold;margin:0;letter-spacing:1px;"">✈️ TripTogether</h2>
        </div>
      </div>

      <div style=""text-align:center;margin-bottom:32px;"">
        <h1 style=""color:#4a90e2;font-size:32px;font-weight:bold;margin:0 0 8px 0;letter-spacing:-0.5px;"">Welcome {request.UserName}!</h1>
        <div style=""width:60px;height:3px;background:linear-gradient(90deg, #4a90e2, #357abd);margin:16px auto;border-radius:2px;""></div>
      </div>

      <div style=""margin-bottom:32px;"">
        <p style=""color:#e5e5e5;font-size:18px;margin:0 0 16px 0;text-align:center;"">You have successfully joined TripTogether! 🎉</p>
        <p style=""color:#b3b3b3;font-size:16px;margin:0;text-align:center;"">Start planning amazing trips with your friends and family. Create groups, plan activities, split expenses, and make unforgettable memories together.</p>
      </div>

      <div style=""background:rgba(74,144,226,0.1);border-left:3px solid #4a90e2;border-radius:8px;padding:20px;margin:24px 0;"">
        <h3 style=""color:#4a90e2;font-size:18px;margin:0 0 12px 0;"">🚀 Get Started:</h3>
        <ul style=""color:#b3b3b3;font-size:15px;margin:0;padding-left:20px;line-height:1.8;"">
          <li>Create or join a group</li>
          <li>Plan your next adventure</li>
          <li>Organize activities and expenses</li>
          <li>Share memories and photos</li>
        </ul>
      </div>

      <div style=""text-align:center;margin:40px 0;"">
        <a href=""https://triptogether.ae-tao-fullstack-api.site"" style=""display:inline-block;background:linear-gradient(135deg, #4a90e2 0%, #357abd 100%);color:#ffffff;padding:16px 32px;text-decoration:none;border-radius:12px;font-weight:bold;font-size:16px;box-shadow:0 8px 24px rgba(74,144,226,0.3);transition:all 0.3s ease;"">Start Planning</a>
      </div>

      <div style=""border-top:1px solid #2d3561;padding-top:24px;margin-top:40px;"">
        <p style=""color:#888888;font-size:14px;margin:0;text-align:center;"">Happy Travels,<br/><span style=""color:#4a90e2;font-weight:600;"">TripTogether Team ✈️</span></p>
      </div>

    </div>
  </body>
</html>";
        await SendEmailAsync(request.To, "Welcome to TripTogether! 🎉", html);
    }

    public async Task SendOtpVerificationEmailAsync(EmailRequestDto request)
    {
        var html = $@"
<html style=""background-color:#0a0e27;margin:0;padding:0;"">
  <body style=""font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;color:#ffffff;padding:40px 20px;background-color:#0a0e27;line-height:1.6;"">
    <div style=""max-width:600px;margin:auto;background:linear-gradient(135deg, #1a1f3a 0%, #2d3561 100%);border:1px solid #4a90e2;border-radius:16px;padding:40px;box-shadow:0 20px 40px rgba(74,144,226,0.15);"">

      <div style=""text-align:center;margin-bottom:32px;"">
        <div style=""display:inline-block;background:linear-gradient(135deg, #4a90e2 0%, #357abd 100%);padding:16px 24px;border-radius:12px;box-shadow:0 8px 20px rgba(74,144,226,0.3);"">
          <h2 style=""color:#ffffff;font-size:28px;font-weight:bold;margin:0;letter-spacing:1px;"">✈️ TripTogether</h2>
        </div>
      </div>

      <div style=""text-align:center;margin-bottom:32px;"">
        <h1 style=""color:#4a90e2;font-size:32px;font-weight:bold;margin:0 0 8px 0;letter-spacing:-0.5px;"">Verify Your Email 🔐</h1>
        <div style=""width:60px;height:3px;background:linear-gradient(90deg, #4a90e2, #357abd);margin:16px auto;border-radius:2px;""></div>
      </div>

      <div style=""margin-bottom:32px;"">
        <p style=""color:#e5e5e5;font-size:16px;margin:0 0 24px 0;text-align:center;"">Thank you for signing up! Please use the following code to verify your email address and start your journey with TripTogether:</p>
      </div>

      <div style=""text-align:center;margin:32px 0;"">
        <div style=""display:inline-block;background:linear-gradient(135deg, #2a2f4a 0%, #3d4571 100%);border:2px solid #4a90e2;border-radius:12px;padding:24px 32px;box-shadow:0 8px 24px rgba(74,144,226,0.25);"">
          <div style=""color:#4a90e2;font-size:36px;font-weight:bold;letter-spacing:8px;font-family:monospace;"">{request.Otp}</div>
        </div>
      </div>

      <div style=""background:rgba(74,144,226,0.1);border:1px solid rgba(74,144,226,0.3);border-radius:8px;padding:16px;margin:24px 0;"">
        <p style=""color:#b3b3b3;font-size:14px;margin:0;text-align:center;"">⏰ This code will expire in <strong style=""color:#4a90e2;"">10 minutes</strong>. If you didn't request this code, please ignore this email.</p>
      </div>

      <div style=""border-top:1px solid #2d3561;padding-top:24px;margin-top:40px;"">
        <p style=""color:#888888;font-size:14px;margin:0;text-align:center;"">Happy Travels,<br/><span style=""color:#4a90e2;font-weight:600;"">TripTogether Team ✈️</span></p>
      </div>

    </div>
  </body>
</html>";
        await SendEmailAsync(request.To, "Verify Your Email - TripTogether", html);
    }

    public async Task SendForgotPasswordOtpEmailAsync(EmailRequestDto request)
    {
        var html = $@"
<html style=""background-color:#0a0e27;margin:0;padding:0;"">
  <body style=""font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;color:#ffffff;padding:40px 20px;background-color:#0a0e27;line-height:1.6;"">
    <div style=""max-width:600px;margin:auto;background:linear-gradient(135deg, #1a1f3a 0%, #2d3561 100%);border:1px solid #4a90e2;border-radius:16px;padding:40px;box-shadow:0 20px 40px rgba(74,144,226,0.15);"">

      <div style=""text-align:center;margin-bottom:32px;"">
        <div style=""display:inline-block;background:linear-gradient(135deg, #4a90e2 0%, #357abd 100%);padding:16px 24px;border-radius:12px;box-shadow:0 8px 20px rgba(74,144,226,0.3);"">
          <h2 style=""color:#ffffff;font-size:28px;font-weight:bold;margin:0;letter-spacing:1px;"">✈️ TripTogether</h2>
        </div>
      </div>

      <div style=""text-align:center;margin-bottom:32px;"">
        <h1 style=""color:#4a90e2;font-size:32px;font-weight:bold;margin:0 0 8px 0;letter-spacing:-0.5px;"">Reset Your Password 🔑</h1>
        <div style=""width:60px;height:3px;background:linear-gradient(90deg, #4a90e2, #357abd);margin:16px auto;border-radius:2px;""></div>
      </div>

      <div style=""margin-bottom:32px;"">
        <p style=""color:#e5e5e5;font-size:16px;margin:0 0 24px 0;text-align:center;"">We received a request to reset your password. Use the following code to proceed with password reset:</p>
      </div>

      <div style=""text-align:center;margin:32px 0;"">
        <div style=""display:inline-block;background:linear-gradient(135deg, #2a2f4a 0%, #3d4571 100%);border:2px solid #4a90e2;border-radius:12px;padding:24px 32px;box-shadow:0 8px 24px rgba(74,144,226,0.25);"">
          <div style=""color:#4a90e2;font-size:36px;font-weight:bold;letter-spacing:8px;font-family:monospace;"">{request.Otp}</div>
        </div>
      </div>

      <div style=""background:rgba(74,144,226,0.1);border:1px solid rgba(74,144,226,0.3);border-radius:8px;padding:16px;margin:24px 0;"">
        <p style=""color:#b3b3b3;font-size:14px;margin:0;text-align:center;"">⏰ This code will expire in <strong style=""color:#4a90e2;"">15 minutes</strong>. If you didn't request a password reset, please ignore this email and your password will remain unchanged.</p>
      </div>

      <div style=""background:rgba(255,107,107,0.1);border-left:3px solid #ff6b6b;border-radius:8px;padding:16px;margin:24px 0;"">
        <p style=""color:#ff6b6b;font-size:14px;margin:0;text-align:center;"">⚠️ For security reasons, never share this code with anyone.</p>
      </div>

      <div style=""border-top:1px solid #2d3561;padding-top:24px;margin-top:40px;"">
        <p style=""color:#888888;font-size:14px;margin:0;text-align:center;"">Happy Travels,<br/><span style=""color:#4a90e2;font-weight:600;"">TripTogether Team ✈️</span></p>
      </div>

    </div>
  </body>
</html>";
        await SendEmailAsync(request.To, "Password Reset - TripTogether", html);
    }

    public async Task SendPasswordChangeSuccessAsync(EmailRequestDto request)
    {
        var html = $@"
<html style=""background-color:#0a0e27;margin:0;padding:0;"">
  <body style=""font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;color:#ffffff;padding:40px 20px;background-color:#0a0e27;line-height:1.6;"">
    <div style=""max-width:600px;margin:auto;background:linear-gradient(135deg, #1a1f3a 0%, #2d3561 100%);border:1px solid #4a90e2;border-radius:16px;padding:40px;box-shadow:0 20px 40px rgba(74,144,226,0.15);"">

      <div style=""text-align:center;margin-bottom:32px;"">
        <div style=""display:inline-block;background:linear-gradient(135deg, #4a90e2 0%, #357abd 100%);padding:16px 24px;border-radius:12px;box-shadow:0 8px 20px rgba(74,144,226,0.3);"">
          <h2 style=""color:#ffffff;font-size:28px;font-weight:bold;margin:0;letter-spacing:1px;"">✈️ TripTogether</h2>
        </div>
      </div>

      <div style=""text-align:center;margin-bottom:32px;"">
        <div style=""display:inline-block;background:rgba(82,196,26,0.15);border-radius:50%;padding:20px;margin-bottom:16px;"">
          <div style=""font-size:48px;"">✅</div>
        </div>
        <h1 style=""color:#52c41a;font-size:32px;font-weight:bold;margin:0 0 8px 0;letter-spacing:-0.5px;"">Password Changed Successfully!</h1>
        <div style=""width:60px;height:3px;background:linear-gradient(90deg, #52c41a, #389e0d);margin:16px auto;border-radius:2px;""></div>
      </div>

      <div style=""margin-bottom:32px;"">
        <p style=""color:#e5e5e5;font-size:18px;margin:0 0 16px 0;text-align:center;"">Hello {request.UserName},</p>
        <p style=""color:#e5e5e5;font-size:16px;margin:0 0 16px 0;text-align:center;"">Your password has been successfully reset for your TripTogether account.</p>
        <p style=""color:#b3b3b3;font-size:16px;margin:0;text-align:center;"">You can now log in with your new password and continue planning amazing trips with your friends.</p>
      </div>

      <div style=""text-align:center;margin:40px 0;"">
        <a href=""https://triptogether.ae-tao-fullstack-api.site/login"" style=""display:inline-block;background:linear-gradient(135deg, #4a90e2 0%, #357abd 100%);color:#ffffff;padding:16px 32px;text-decoration:none;border-radius:12px;font-weight:bold;font-size:16px;box-shadow:0 8px 24px rgba(74,144,226,0.3);transition:all 0.3s ease;"">Login Now</a>
      </div>

      <div style=""background:rgba(255,107,107,0.1);border:1px solid rgba(255,107,107,0.3);border-radius:8px;padding:16px;margin:32px 0;"">
        <p style=""color:#ff6b6b;font-size:14px;margin:0;text-align:center;"">⚠️ If you didn't make this change or have concerns about your account security, please contact our support team immediately.</p>
      </div>

      <div style=""border-top:1px solid #2d3561;padding-top:24px;margin-top:40px;"">
        <p style=""color:#888888;font-size:14px;margin:0;text-align:center;"">Happy Travels,<br/><span style=""color:#4a90e2;font-weight:600;"">TripTogether Team ✈️</span></p>
      </div>

    </div>
  </body>
</html>";
        await SendEmailAsync(request.To, "Password Changed Successfully - TripTogether", html);
    }

}
