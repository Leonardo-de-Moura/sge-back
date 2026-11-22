using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SgeIfce.Api.Services;
using Xunit;

namespace SgeIfce.Api.Tests;

public class ResendEmailServiceTests
{
    [Fact]
    public async Task SendAsync_PostsExpectedResendPayloadAndAuthorization()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var httpClient = new HttpClient(new StubHttpMessageHandler(async request =>
        {
            capturedRequest = request;
            capturedBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        var service = CreateService(httpClient);

        await service.SendAsync("user@ifce.edu.br", "Assunto", "<p>Olá</p>");

        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal("https://api.resend.com/emails", capturedRequest.RequestUri!.ToString());
        Assert.Equal("Bearer", capturedRequest.Headers.Authorization!.Scheme);
        Assert.Equal("resend-test-key", capturedRequest.Headers.Authorization.Parameter);
        using var payload = JsonDocument.Parse(capturedBody!);
        Assert.Equal("noreply@ifce.edu.br", payload.RootElement.GetProperty("from").GetString());
        Assert.Equal("user@ifce.edu.br", payload.RootElement.GetProperty("to")[0].GetString());
        Assert.Equal("Assunto", payload.RootElement.GetProperty("subject").GetString());
        Assert.Equal("<p>Olá</p>", payload.RootElement.GetProperty("html").GetString());
    }

    [Fact]
    public async Task SendAsync_WhenResendRejectsRequest_IncludesResponseBodyInException()
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"message\":\"invalid sender\"}")
            })));
        var service = CreateService(httpClient);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.SendAsync("user@ifce.edu.br", "Assunto", "<p>Olá</p>"));

        Assert.Contains("invalid sender", exception.Message);
    }

    private static ResendEmailService CreateService(HttpClient httpClient)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:ApiKey"] = "resend-test-key",
                ["Email:From"] = "noreply@ifce.edu.br"
            })
            .Build();
        return new ResendEmailService(httpClient, config);
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> sendAsync) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => sendAsync(request);
    }
}
