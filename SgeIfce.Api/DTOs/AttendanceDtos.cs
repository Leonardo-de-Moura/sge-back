using System.ComponentModel.DataAnnotations;

namespace SgeIfce.Api.DTOs;

public class ParticipantAttendanceDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Matricula { get; set; } = string.Empty;
    public string Status { get; set; } = "pendente"; // presente, ausente, pendente
    public bool CertificateIssued { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime? CheckedInAt { get; set; }
}

public class UpdateAttendanceDto
{
    [Required(ErrorMessage = "O status de presença é obrigatório.")]
    public string Status { get; set; } = "presente"; // presente, ausente, pendente
}

public class BulkAttendanceItemDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public string Status { get; set; } = "presente";
}

public class BulkAttendanceDto
{
    [Required]
    public Guid EventId { get; set; }

    [Required]
    public List<BulkAttendanceItemDto> Attendances { get; set; } = new();
}
