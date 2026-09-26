using System.ComponentModel.DataAnnotations;

namespace SgeIfce.Api.DTOs;

public class EventResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Modality { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string DayMonth { get; set; } = string.Empty;
    public string Workload { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int TotalSlots { get; set; }
    public int EnrolledSlots { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public List<ActivityDto> Activities { get; set; } = new();
}

public class ActivityDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string? Speaker { get; set; }
    public string? Location { get; set; }
}

public class CreateEventDto
{
    [Required(ErrorMessage = "O título do evento é obrigatório.")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "A descrição é obrigatória.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "A categoria é obrigatória.")]
    public string Category { get; set; } = "Palestra";

    [Required(ErrorMessage = "A modalidade é obrigatória.")]
    public string Modality { get; set; } = "Presencial";

    public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(7);
    public DateTime? EndDate { get; set; }

    [MaxLength(30)]
    public string? DayMonth { get; set; }

    public string Workload { get; set; } = "4 horas";

    [Required(ErrorMessage = "O local é obrigatório.")]
    public string Location { get; set; } = "Auditório Principal - Campus Cedro";

    [Range(1, 10000, ErrorMessage = "O número de vagas deve ser no mínimo 1.")]
    public int TotalSlots { get; set; } = 50;

    public List<CreateActivityDto>? Activities { get; set; }
}

public class CreateActivityDto
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Time { get; set; } = string.Empty;

    public string? Speaker { get; set; }
    public string? Location { get; set; }
}

public class UpdateEventDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Modality { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Workload { get; set; }
    public string? Location { get; set; }
    public int? TotalSlots { get; set; }
    public string? Status { get; set; }
}

public class DashboardStatsDto
{
    public int EventsUnderManagement { get; set; }
    public int ActiveEventsCount { get; set; }
    public int TotalEnrolledCount { get; set; }
    public int TodayNewEnrolledCount { get; set; }
    public double ConfirmedAttendancePercentage { get; set; }
    public int IssuedCertificatesCount { get; set; }
    public int PendingCertificatesCount { get; set; }
}
