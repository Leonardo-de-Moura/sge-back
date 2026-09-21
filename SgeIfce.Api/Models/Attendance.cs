using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SgeIfce.Api.Models;

public class Attendance
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

    public Guid? RegistrationId { get; set; }

    [ForeignKey(nameof(RegistrationId))]
    public Registration? Registration { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "pendente"; // presente, ausente, pendente

    public bool CertificateIssued { get; set; } = false;

    public DateTime? CheckedInAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Certificate? Certificate { get; set; }
}
