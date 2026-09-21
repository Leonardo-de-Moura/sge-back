using System.ComponentModel.DataAnnotations;

namespace SgeIfce.Api.DTOs;

public class CertificateResponseDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string IssueDate { get; set; } = string.Empty;
    public string Workload { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ParticipantName { get; set; } = string.Empty;
}

public class ValidateCertificateDto
{
    public bool Valid { get; set; }
    public string? Code { get; set; }
    public string? ParticipantName { get; set; }
    public string? EventTitle { get; set; }
    public string? Workload { get; set; }
    public string? IssueDate { get; set; }
    public string? Institution { get; set; } = "Instituto Federal do Ceará - IFCE Campus Cedro";
}

public class IssueCertificateDto
{
    [Required(ErrorMessage = "O ID do evento é obrigatório.")]
    public Guid EventId { get; set; }

    public List<Guid>? AttendanceIds { get; set; }
}

public class IssueResultDto
{
    public int TotalIssued { get; set; }
    public List<string> IssuedCodes { get; set; } = new();
}
