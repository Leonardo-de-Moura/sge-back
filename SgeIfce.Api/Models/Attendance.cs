using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SgeIfce.Api.Models;

public class Attendance
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
    [MaxLength(150)]
    public string ParticipantName { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string ParticipantEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Matricula { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "pendente";

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    public bool CertificateIssued { get; set; } = false;

    public DateTime? CheckedInAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}