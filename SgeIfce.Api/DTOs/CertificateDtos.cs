using System.ComponentModel.DataAnnotations;

namespace SgeIfce.Api.DTOs;

public class CertificateResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public string IssueDate { get; set; } = string.Empty;
    public string Workload { get; set; } = string.Empty;
    public string ValidationCode { get; set; } = string.Empty;
    public string ParticipantName { get; set; } = string.Empty;
}

public class ValidateCertificateDto
{
    public bool Valid { get; set; }
    public string? ValidationCode { get; set; }
    public string? ParticipantName { get; set; }
    public string? EventTitle { get; set; }
    public string? Workload { get; set; }
    public string? IssueDate { get; set; }
    public string? Institution { get; set; } = "Instituto Federal do Ceará - IFCE Campus Cedro";
}

public class IssueCertificateDto
{
    [Required(ErrorMessage = "O ID do evento é obrigatório.")]
    public string EventId { get; set; } = string.Empty;

    public List<string>? AttendanceIds { get; set; }
}

public class IssueResultDto
{
    public int TotalIssued { get; set; }
    public List<string> IssuedCodes { get; set; } = new();
}

public class CertificateDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public string Workload { get; set; } = string.Empty;
    public string ValidationCode { get; set; } = string.Empty;
    public string IssueDate { get; set; } = string.Empty;
    public string Institution { get; set; } = "Instituto Federal do Ceará - IFCE Campus Cedro";
}
