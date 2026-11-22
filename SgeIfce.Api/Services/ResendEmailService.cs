using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SgeIfce.Api.Services;

public sealed class ResendEmailService : IEmailService
{
    private static readonly Uri EmailsEndpoint = new("https://api.resend.com/emails");

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public ResendEmailService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public Task SendAsync(string to, string subject, string html)
        => SendAsync(to, subject, html, CancellationToken.None);

    public async Task SendAsync(
        string to,
        string subject,
        string html,
        CancellationToken cancellationToken)
    {
        var apiKey = _configuration["Email:ApiKey"];
        var from = _configuration["Email:From"];

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(from))
        {
            throw new InvalidOperationException(
                "Email:ApiKey e Email:From precisam estar configurados.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, EmailsEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new
        {
            from,
            to = new[] { to },
            subject,
            html
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"Falha ao enviar e-mail pelo Resend ({(int)response.StatusCode}): {responseBody}",
            null,
            response.StatusCode);
    }
}
