namespace SgeIfce.Api.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string html);

    Task SendAsync(
        string to,
        string subject,
        string html,
        CancellationToken cancellationToken) => SendAsync(to, subject, html);
}
