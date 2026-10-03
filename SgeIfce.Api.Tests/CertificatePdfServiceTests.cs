using System.Text;
using SgeIfce.Api.Models;
using SgeIfce.Api.Services;
using Xunit;

namespace SgeIfce.Api.Tests;

public class CertificatePdfServiceTests
{
    [Fact]
    public void GeneratePdf_CreatesLandscapeCertificateWithIssuedData()
    {
        var certificate = new Certificate
        {
            ParticipantName = "João da Silva",
            EventTitle = "Jornada de Ciência e Tecnologia",
            Workload = "12 horas",
            ValidationCode = "IFCE-CED-2026-CERT-ABC123",
            IssueDate = new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc),
            Event = new Event
            {
                Organizer = new User { Name = "Luna de Sousa" }
            }
        };

        var pdf = new CertificatePdfService().GeneratePdf(certificate);
        var pdfText = Encoding.ASCII.GetString(pdf);

        Assert.StartsWith("%PDF-1.4", pdfText);
        Assert.Contains("/MediaBox [0 0 842 595]", pdfText);
        Assert.Contains(Convert.ToHexString(Encoding.Latin1.GetBytes("CERTIFICADO")), pdfText);
        Assert.Contains(Convert.ToHexString(Encoding.Latin1.GetBytes("IFCE-CED-2026-CERT-ABC123")), pdfText);
        Assert.Contains(Convert.ToHexString(Encoding.Latin1.GetBytes("João da Silva")), pdfText);
        Assert.Contains(Convert.ToHexString(Encoding.Latin1.GetBytes("Luna de Sousa")), pdfText);
        Assert.Contains("/BaseFont /ZapfChancery-MediumItalic", pdfText);
        Assert.Contains("/BaseFont /Times-Bold", pdfText);
        Assert.Contains("%%EOF", pdfText);
    }

    [Fact]
    public void GeneratePdf_RejectsNullCertificate()
    {
        var service = new CertificatePdfService();

        Assert.Throws<ArgumentNullException>(() => service.GeneratePdf(null!));
    }
}
