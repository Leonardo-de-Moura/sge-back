using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SgeIfce.Api.Models;

public class Event
{
    [Key]
    [MaxLength(64)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(60)]
    public string Category { get; set; } = "Palestra";

    [Required]
    [MaxLength(30)]
    public string Modality { get; set; } = "Presencial";

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Required]
    [MaxLength(50)]
    public string Workload { get; set; } = "4 horas";

    [Required]
    [MaxLength(255)]
    public string Location { get; set; } = "IFCE Campus Cedro";

    public int TotalSlots { get; set; }

    public int EnrolledSlots { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Aberto";

    [Required]
    [MaxLength(30)]
    public string DayMonth { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    [MaxLength(255)]
    public string? QrCodeToken { get; set; }

    public DateTime? QrCodeGeneratedAt { get; set; }

    public DateTime? QrCodeExpiresAt { get; set; }

    [MaxLength(64)]
    public string? OrganizerId { get; set; }

    [ForeignKey(nameof(OrganizerId))]
    public User? Organizer { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<Activity> Activities { get; set; } = new List<Activity>();

    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();

    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
}