namespace SgeIfce.Api.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string htmlBody);
    Task SendPasswordResetAsync(string toEmail, string resetLink);
    Task SendAccountConfirmationAsync(string toEmail, string confirmationLink);
}
