using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SgeIfce.Api.Models;

public class Certificate
{
    [Key]
    [MaxLength(64)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string EventId { get; set; } = string.Empty;

    [ForeignKey(nameof(EventId))]
    public Event? Event { get; set; }

    [MaxLength(64)]
    public string? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Required]
    [MaxLength(255)]
    public string EventTitle { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string ParticipantName { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string ParticipantEmail { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Matricula { get; set; }

    public DateTime IssueDate { get; set; }

    [Required]
    [MaxLength(50)]
    public string Workload { get; set; } = "4 horas";

    [Required]
    [MaxLength(80)]
    public string ValidationCode { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? PdfUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}