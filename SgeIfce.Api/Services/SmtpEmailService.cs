using System.Net;
using System.Net.Mail;

namespace SgeIfce.Api.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var host = _configuration["Email:Host"];
        var portText = _configuration["Email:Port"];
        var username = _configuration["Email:Username"];
        var password = _configuration["Email:Password"];
        var from = _configuration["Email:From"];

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(from) ||
            string.IsNullOrWhiteSpace(username))
        {
            _logger.LogWarning(
                "Configuração de e-mail não encontrada. O envio foi ignorado."
            );
            return;
        }

        var port = int.TryParse(portText, out var parsedPort) ? parsedPort : 587;

        using var message = new MailMessage
        {
            From = new MailAddress(from, _configuration["Email:FromName"] ?? "SGE-IFCE"),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        message.To.Add(toEmail);

        using var smtpClient = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(username, password),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        await smtpClient.SendMailAsync(message);
    }

    public Task SendPasswordResetAsync(string toEmail, string resetLink)
    {
        var body = BuildEmailTemplate(
            title: "Recuperação de senha",
            intro: "Você solicitou a recuperação da sua senha no SGE-IFCE.",
            description: "Clique no botão abaixo para criar uma nova senha. Este link é válido por 30 minutos.",
            ctaText: "Redefinir senha",
            ctaLink: resetLink,
            footer: "Se você não solicitou essa alteração, ignore este e-mail."
        );

        return SendEmailAsync(toEmail, "Recuperação de senha - SGE-IFCE", body);
    }

    public Task SendAccountConfirmationAsync(string toEmail, string confirmationLink)
    {
        var body = BuildEmailTemplate(
            title: "Confirme seu cadastro",
            intro: "Seu cadastro no SGE-IFCE foi realizado com sucesso.",
            description: "Para ativar sua conta e começar a usar o sistema, confirme seu endereço de e-mail clicando no botão abaixo.",
            ctaText: "Confirmar e-mail",
            ctaLink: confirmationLink,
            footer: "Se você não criou esta conta, ignore este e-mail."
        );

        return SendEmailAsync(toEmail, "Confirmação de cadastro - SGE-IFCE", body);
    }

    private static string BuildEmailTemplate(
        string title,
        string intro,
        string description,
        string ctaText,
        string ctaLink,
        string footer)
    {
        return $@"
            <html>
              <body style=""margin:0;padding:0;background-color:#f5f7f8;font-family:Arial,Helvetica,sans-serif;"">
                <div style=""max-width:640px;margin:0 auto;padding:32px 16px;"">
                  <div style=""background:#ffffff;border:1px solid #e7e7e7;border-radius:16px;overflow:hidden;"">
                    <div style=""background:#006A38;padding:24px 32px;color:#ffffff;"">
                      <h2 style=""margin:0;font-size:24px;"">{title}</h2>
                    </div>
                    <div style=""padding:32px 28px;color:#1f2937;line-height:1.7;"">
                      <p style=""margin:0 0 16px;font-size:16px;"">{intro}</p>
                      <p style=""margin:0 0 24px;font-size:15px;"">{description}</p>
                      <div style=""text-align:center;margin:24px 0;"">
                        <a href=""{ctaLink}"" style=""display:inline-block;background:#006A38;color:#ffffff;text-decoration:none;padding:14px 28px;border-radius:10px;font-weight:bold;"">{ctaText}</a>
                      </div>
                      <p style=""margin:0 0 8px;font-size:12px;color:#4b5563;"">Se o botão não funcionar, copie e cole este link no navegador:</p>
                      <p style=""margin:0;font-size:12px;color:#1f2937;word-break:break-all;"">{ctaLink}</p>
                    </div>
                    <div style=""padding:20px 28px;border-top:1px solid #e7e7e7;background:#f9fafb;color:#4b5563;font-size:12px;"">
                      {footer}
                    </div>
                  </div>
                </div>
              </body>
            </html>
        ";
    }
}
