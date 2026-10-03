using System.ComponentModel.DataAnnotations;

namespace SgeIfce.Api.DTOs;

public class ParticipantAttendanceDto
{
    public string Id { get; set; } = string.Empty;
    public string? UserId { get; set; }
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
    public string Id { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = "presente";
}

public class BulkAttendanceDto
{
    [Required]
    public string EventId { get; set; } = string.Empty;

    [Required]
    public List<BulkAttendanceItemDto> Attendances { get; set; } = new();
}

public class CheckInRequestDto
{
    [Required(ErrorMessage = "O token do QR Code é obrigatório.")]
    public string Token { get; set; } = string.Empty;
}

public class CheckInResponseDto
{
    public string EventId { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CheckedInAt { get; set; }
    public bool AlreadyRegistered { get; set; }
}
