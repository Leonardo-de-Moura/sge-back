using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SgeIfce.Api.Controllers;
using SgeIfce.Api.Data;
using SgeIfce.Api.DTOs;
using SgeIfce.Api.Models;
using SgeIfce.Api.Services;
using Xunit;

namespace SgeIfce.Api.Tests;

public class CertificatesControllerOwnershipTests
{
    [Fact]
    public async Task IssueCertificates_NonOwnerCannotIssueCertificates()
    {
        await using var context = CreateContext();
        await SeedEventAndCertificateAsync(context);
        var controller = CreateController(context);

        var result = await controller.IssueCertificates(new IssueCertificateDto { EventId = "event-1" });

        Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Single(await context.Certificates.ToListAsync());
    }

    [Fact]
    public async Task GetCertificateById_NonOwnerProfessorCannotReadCertificateDetails()
    {
        await using var context = CreateContext();
        await SeedEventAndCertificateAsync(context);
        var controller = CreateController(context);

        var result = await controller.GetCertificateById("certificate-1");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task DownloadCertificatePdf_NonOwnerProfessorCannotDownloadCertificate()
    {
        await using var context = CreateContext();
        await SeedEventAndCertificateAsync(context);
        var controller = CreateController(context);

        var result = await controller.DownloadCertificatePdf("certificate-1");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    private static async Task SeedEventAndCertificateAsync(AppDbContext context)
    {
        context.Events.Add(new Event
        {
            Id = "event-1",
            Title = "Evento",
            Description = "Descrição",
            Category = "Palestra",
            Modality = "Presencial",
            Workload = "2 horas",
            Location = "Campus",
            TotalSlots = 10,
            Status = "Aberto",
            DayMonth = "04 NOV",
            OrganizerId = "teacher-1"
        });
        context.Certificates.Add(new Certificate
        {
            Id = "certificate-1",
            EventId = "event-1",
            UserId = "student-1",
            EventTitle = "Evento",
            ParticipantName = "Aluno",
            ParticipantEmail = "aluno@ifce.edu.br",
            Workload = "2 horas",
            ValidationCode = "validation-code",
            IssueDate = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static CertificatesController CreateController(AppDbContext context)
    {
        var controller = new CertificatesController(context, new NeverCalledPdfService());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "teacher-2"),
                    new Claim(ClaimTypes.Role, "Professor")
                ], "Test"))
            }
        };
        return controller;
    }

    private sealed class NeverCalledPdfService : ICertificatePdfService
    {
        public byte[] GeneratePdf(Certificate certificate) =>
            throw new InvalidOperationException("PDF generation should not be reached for a non-owner.");
    }
}
