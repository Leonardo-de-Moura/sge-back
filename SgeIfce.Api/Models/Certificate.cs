using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SgeIfce.Api.Models;

public class Certificate
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid EventId { get; set; }

    [ForeignKey(nameof(EventId))]
    public Event? Event { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    public Guid? AttendanceId { get; set; }

    [ForeignKey(nameof(AttendanceId))]
    public Attendance? Attendance { get; set; }

    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty; // Ex: "IFCE-CED-2026-CERT-8841"

    [Required]
    [MaxLength(200)]
    public string EventTitle { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string ParticipantName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Workload { get; set; } = "4 horas";

    public DateTime IssueDate { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
