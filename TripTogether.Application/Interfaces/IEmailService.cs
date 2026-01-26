public interface IEmailService
{
    Task SendRegistrationSuccessEmailAsync(EmailRequestDto request);

    Task SendOtpVerificationEmailAsync(EmailRequestDto request);

    Task SendForgotPasswordOtpEmailAsync(EmailRequestDto request);

    Task SendPasswordChangeSuccessAsync(EmailRequestDto request);

}