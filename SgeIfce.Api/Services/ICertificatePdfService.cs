using System.Globalization;
using System.Text;
using SgeIfce.Api.Models;

namespace SgeIfce.Api.Services;

public interface ICertificatePdfService
{
    byte[] GeneratePdf(Certificate certificate);
}

public class CertificatePdfService : ICertificatePdfService
{
    private const double PageWidth = 842;
    private const double PageHeight = 595;

    public byte[] GeneratePdf(Certificate certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        var content = new StringBuilder();
        var issueDate = certificate.IssueDate.ToString("dd 'DE' MMMM 'DE' yyyy", CultureInfo.GetCultureInfo("pt-BR")).ToUpperInvariant();

        DrawRectangle(content, 0, 0, PageWidth, PageHeight, "#FFFFFF");
        DrawRectangle(content, 9, 9, PageWidth - 18, PageHeight - 18, "#FFFFFF", "#1F6A27", 1.3);
        DrawRectangle(content, 41, 36, 772, 528, "#FFFFFF", "#B20D12", 2.4);
        DrawLine(content, 41, 564, 207, 564, "#FFFFFF", 3.5);
        DrawLine(content, 41, 401, 41, 564, "#FFFFFF", 3.5);
        DrawLine(content, 619, 36, 813, 36, "#FFFFFF", 3.5);
        DrawLine(content, 813, 36, 813, 222, "#FFFFFF", 3.5);

        DrawPolygon(content, new[] { (12d, 586d), (198d, 586d), (12d, 400d) }, "#69A64C");
        DrawPolygon(content, new[] { (12d, 586d), (12d, 465d), (105d, 438d) }, "#08751A");
        DrawPolygon(content, new[] { (842d, 10d), (842d, 208d), (644d, 10d) }, "#69A64C");
        DrawPolygon(content, new[] { (842d, 10d), (842d, 131d), (749d, 158d) }, "#08751A");
        DrawLine(content, 619, 10, 842, 233, "#B20D12", 3);

        DrawCertificateMark(content, 155, 528);
        DrawText(content, "INSTITUTO FEDERAL", 210, 537, 13, bold: true, color: "#172C2D");
        DrawText(content, "Ceará", 210, 520, 11, color: "#315E35");
        DrawText(content, "Campus Cedro", 210, 506, 11, color: "#315E35");

        DrawText(content, "CERTIFICADO", PageWidth / 2, 402, 47, bold: true, color: "#08751A", centered: true, font: "F4");
        DrawText(content, "INSTITUTO FEDERAL DE EDUCAÇÃO, CIÊNCIA E TECNOLOGIA DO", PageWidth / 2, 367, 11, color: "#191919", centered: true);
        DrawText(content, "CEARÁ - CAMPUS CEDRO CERTIFICA QUE", PageWidth / 2, 340, 11, color: "#191919", centered: true);

        var participantFontSize = Math.Min(37, 440 / Math.Max(1, certificate.ParticipantName.Length * 0.48));
        DrawText(content, certificate.ParticipantName, PageWidth / 2, 265, participantFontSize, color: "#08751A", centered: true, font: "F3");
        DrawLine(content, 180, 243, 662, 243, "#69A64C", 1.2);

        var eventTitle = certificate.EventTitle.Trim().ToUpperInvariant();
        DrawFittedCenteredText(content, $"CONCLUIU O CURSO DE {eventTitle},", PageWidth / 2, 223, 11, 700);
        DrawFittedCenteredText(content, $"COM CARGA HORÁRIA DE {certificate.Workload.ToUpperInvariant()}, REALIZADO NO DIA {issueDate}, NO", PageWidth / 2, 197, 11, 700);
        DrawText(content, "ÂMBITO DO SISTEMA DE GESTÃO DE EVENTOS DO IFCE", PageWidth / 2, 170, 11, color: "#191919", centered: true);

        DrawSignature(content, 421, 127, certificate.Event?.Organizer?.Name ?? "Coordenação de Eventos");
        DrawLine(content, 274, 107, 568, 107, "#174C9B", 1.2);
        DrawText(content, "Coordenador do Evento", PageWidth / 2, 81, 11, bold: true, color: "#111111", centered: true);
        DrawText(content, "IFCE - Campus Cedro", PageWidth / 2, 57, 10, color: "#111111", centered: true);
        DrawText(content, $"CÓDIGO DE VALIDAÇÃO: {certificate.ValidationCode}", 115, 21, 5.5, color: "#536257");

        return BuildPdf(content.ToString());
    }

    private static void DrawCertificateMark(StringBuilder content, double x, double y)
    {
        DrawCircle(content, x + 7, y + 32, 6, "#C8102E", "#C8102E", 0.5);
        var blocks = new[]
        {
            (x + 20, y + 26), (x + 36, y + 26),
            (x + 4, y + 10), (x + 20, y + 10),
            (x + 4, y - 6), (x + 20, y - 6), (x + 36, y - 6),
            (x + 4, y - 22), (x + 20, y - 22)
        };

        foreach (var (blockX, blockY) in blocks)
        {
            DrawRectangle(content, blockX, blockY, 12, 12, "#397A13");
        }
    }

    private static void DrawSignature(StringBuilder content, double centerX, double y, string signerName)
    {
        var signatureFontSize = Math.Min(31, 280 / Math.Max(1, signerName.Length * 0.48));
        DrawText(content, signerName, centerX, y, signatureFontSize, color: "#161616", centered: true, font: "F3");
    }

    private static void DrawRectangle(
        StringBuilder content,
        double x,
        double y,
        double width,
        double height,
        string fill,
        string? stroke = null,
        double lineWidth = 1)
    {
        if (stroke is not null)
        {
            content.Append(Number(lineWidth)).Append(" w ").Append(Color(stroke)).Append(" RG ");
        }

        content.Append(Color(fill)).Append(" rg ")
            .Append(Number(x)).Append(' ').Append(Number(y)).Append(' ')
            .Append(Number(width)).Append(' ').Append(Number(height)).Append(" re ")
            .Append(stroke is null ? "f\n" : "B\n");
    }

    private static void DrawCircle(
        StringBuilder content,
        double centerX,
        double centerY,
        double radius,
        string fill,
        string stroke,
        double lineWidth)
    {
        const double curve = 0.5522847498;
        var offset = radius * curve;
        content.Append(Number(lineWidth)).Append(" w ").Append(Color(stroke)).Append(" RG ")
            .Append(Color(fill)).Append(" rg ")
            .Append(Number(centerX + radius)).Append(' ').Append(Number(centerY)).Append(" m ")
            .Append(Number(centerX + radius)).Append(' ').Append(Number(centerY + offset)).Append(' ')
            .Append(Number(centerX + offset)).Append(' ').Append(Number(centerY + radius)).Append(' ')
            .Append(Number(centerX)).Append(' ').Append(Number(centerY + radius)).Append(" c ")
            .Append(Number(centerX - offset)).Append(' ').Append(Number(centerY + radius)).Append(' ')
            .Append(Number(centerX - radius)).Append(' ').Append(Number(centerY + offset)).Append(' ')
            .Append(Number(centerX - radius)).Append(' ').Append(Number(centerY)).Append(" c ")
            .Append(Number(centerX - radius)).Append(' ').Append(Number(centerY - offset)).Append(' ')
            .Append(Number(centerX - offset)).Append(' ').Append(Number(centerY - radius)).Append(' ')
            .Append(Number(centerX)).Append(' ').Append(Number(centerY - radius)).Append(" c ")
            .Append(Number(centerX + offset)).Append(' ').Append(Number(centerY - radius)).Append(' ')
            .Append(Number(centerX + radius)).Append(' ').Append(Number(centerY - offset)).Append(' ')
            .Append(Number(centerX + radius)).Append(' ').Append(Number(centerY)).Append(" c B\n");
    }

    private static void DrawText(
        StringBuilder content,
        string text,
        double x,
        double y,
        double size,
        bool bold = false,
        string color = "#000000",
        bool centered = false,
        string? font = null)
    {
        var normalized = NormalizePdfText(text);
        font ??= bold ? "F2" : "F1";
        var estimatedWidth = EstimateTextWidth(normalized, size, font);
        var textX = centered ? x - estimatedWidth / 2 : x;

        content.Append("BT /").Append(font).Append(' ')
            .Append(Number(size)).Append(" Tf ")
            .Append(Color(color)).Append(" rg ")
            .Append(Number(textX)).Append(' ').Append(Number(y)).Append(" Td <")
            .Append(Convert.ToHexString(Encoding.Latin1.GetBytes(normalized)))
            .Append("> Tj ET\n");
    }

    private static void DrawFittedCenteredText(StringBuilder content, string text, double centerX, double y, double size, double maxWidth)
    {
        var fittedSize = Math.Min(size, maxWidth / Math.Max(1, text.Length * 0.5));
        DrawText(content, text, centerX, y, fittedSize, color: "#191919", centered: true);
    }

    private static double EstimateTextWidth(string text, double size, string font)
    {
        var factor = font switch
        {
            "F3" => 0.48,
            "F4" => 0.55,
            "F2" => 0.52,
            _ => 0.5
        };
        return text.Length * size * factor;
    }

    private static void DrawLine(StringBuilder content, double x1, double y1, double x2, double y2, string color, double width)
    {
        content.Append(Number(width)).Append(" w ").Append(Color(color)).Append(" RG ")
            .Append(Number(x1)).Append(' ').Append(Number(y1)).Append(" m ")
            .Append(Number(x2)).Append(' ').Append(Number(y2)).Append(" l S\n");
    }

    private static void DrawPolygon(StringBuilder content, IReadOnlyList<(double X, double Y)> points, string fill)
    {
        if (points.Count < 3)
        {
            throw new ArgumentException("A polygon requires at least three points.", nameof(points));
        }

        content.Append(Color(fill)).Append(" rg ")
            .Append(Number(points[0].X)).Append(' ').Append(Number(points[0].Y)).Append(" m ");
        foreach (var point in points.Skip(1))
        {
            content.Append(Number(point.X)).Append(' ').Append(Number(point.Y)).Append(" l ");
        }

        content.Append("h f\n");
    }

    private static string NormalizePdfText(string text)
    {
        var result = new StringBuilder(text.Length);
        foreach (var character in text.Normalize(NormalizationForm.FormC))
        {
            result.Append(character <= 255 ? character : '?');
        }

        return result.ToString();
    }

    private static string Color(string hex)
    {
        var red = Convert.ToInt32(hex[1..3], 16) / 255d;
        var green = Convert.ToInt32(hex[3..5], 16) / 255d;
        var blue = Convert.ToInt32(hex[5..7], 16) / 255d;
        return $"{Number(red)} {Number(green)} {Number(blue)}";
    }

    private static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static byte[] BuildPdf(string content)
    {
        var contentBytes = Encoding.ASCII.GetBytes(content);
        var objects = new[]
        {
            Encoding.ASCII.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"),
            Encoding.ASCII.GetBytes("<< /Type /Pages /Kids [3 0 R] /Count 1 >>"),
            Encoding.ASCII.GetBytes(
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {Number(PageWidth)} {Number(PageHeight)}] " +
                "/Resources << /Font << /F1 4 0 R /F2 5 0 R /F3 6 0 R /F4 7 0 R >> >> /Contents 8 0 R >>"),
            Encoding.ASCII.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Times-Roman /Encoding /WinAnsiEncoding >>"),
            Encoding.ASCII.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Times-Bold /Encoding /WinAnsiEncoding >>"),
            Encoding.ASCII.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /ZapfChancery-MediumItalic >>"),
            Encoding.ASCII.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Times-Bold /Encoding /WinAnsiEncoding >>"),
            JoinBytes(
                Encoding.ASCII.GetBytes($"<< /Length {contentBytes.Length} >>\nstream\n"),
                contentBytes,
                Encoding.ASCII.GetBytes("\nendstream"))
        };

        using var output = new MemoryStream();
        WriteAscii(output, "%PDF-1.4\n");
        var offsets = new List<long> { 0 };

        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(output.Position);
            WriteAscii(output, $"{index + 1} 0 obj\n");
            output.Write(objects[index]);
            WriteAscii(output, "\nendobj\n");
        }

        var xrefOffset = output.Position;
        WriteAscii(output, $"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            WriteAscii(output, $"{offset:D10} 00000 n \n");
        }

        WriteAscii(output,
            $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        return output.ToArray();
    }

    private static byte[] JoinBytes(params byte[][] arrays)
    {
        using var stream = new MemoryStream();
        foreach (var array in arrays)
        {
            stream.Write(array);
        }

        return stream.ToArray();
    }

    private static void WriteAscii(Stream stream, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        stream.Write(bytes);
    }
}
