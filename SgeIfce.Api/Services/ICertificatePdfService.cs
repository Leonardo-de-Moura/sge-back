using System.Text;
using SgeIfce.Api.Models;

namespace SgeIfce.Api.Services;

public interface ICertificatePdfService
{
    byte[] GeneratePdf(Certificate certificate);
}

public class CertificatePdfService : ICertificatePdfService
{
    public byte[] GeneratePdf(Certificate certificate)
    {
        // Gera um PDF compatível padrão PDF-1.4 sem dependências externas pesadas
        var title = certificate.EventTitle;
        var student = certificate.ParticipantName;
        var workload = certificate.Workload;
        var code = certificate.Code;
        var date = certificate.IssueDate.ToString("dd/MM/yyyy");

        var textContent = $"""
            BT
            /F1 24 Tf
            50 720 Td
            (INSTITUTO FEDERAL DO CEARA - CAMPUS CEDRO) Tj
            /F1 16 Tf
            0 -40 Td
            (CERTIFICADO DE PARTICIPACAO EM EVENTO ACADEMICO) Tj
            /F1 12 Tf
            0 -50 Td
            (Certificamos que {EscapePdfText(student)} participou do evento:) Tj
            /F1 14 Tf
            0 -25 Td
            ("{EscapePdfText(title)}") Tj
            /F1 12 Tf
            0 -30 Td
            (Carga Horaria Homologada: {EscapePdfText(workload)}) Tj
            0 -20 Td
            (Data de Emissao: {date}) Tj
            0 -50 Td
            (Codigo Verificador Oficial: {EscapePdfText(code)}) Tj
            /F1 10 Tf
            0 -25 Td
            (Autenticidade verificavel no Portal SGE-IFCE atraves do codigo acima.) Tj
            0 -40 Td
            (Diretoria de Ensino e Pesquisa - Coordenacao de Extensao IFCE) Tj
            ET
            """;

        var streamBytes = Encoding.ASCII.GetBytes(textContent);
        var streamLength = streamBytes.Length;

        var pdfBuilder = new StringBuilder();
        pdfBuilder.Append("%PDF-1.4\n");
        pdfBuilder.Append("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdfBuilder.Append("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        pdfBuilder.Append("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>\nendobj\n");
        pdfBuilder.Append($"4 0 obj\n<< /Length {streamLength} >>\nstream\n");
        pdfBuilder.Append(textContent);
        pdfBuilder.Append("\nendstream\nendobj\n");
        pdfBuilder.Append("5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>\nendobj\n");
        pdfBuilder.Append("xref\n0 6\n");
        pdfBuilder.Append("0000000000 65535 f \n");
        pdfBuilder.Append("0000000009 00000 n \n");
        pdfBuilder.Append("0000000058 00000 n \n");
        pdfBuilder.Append("0000000115 00000 n \n");
        pdfBuilder.Append("0000000250 00000 n \n");
        pdfBuilder.Append("0000000450 00000 n \n");
        pdfBuilder.Append("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n520\n%%EOF\n");

        return Encoding.ASCII.GetBytes(pdfBuilder.ToString());
    }

    private static string EscapePdfText(string text)
    {
        return text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }
}
