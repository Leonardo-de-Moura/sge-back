using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SgeIfce.Api.Models;

public class Event
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string Category { get; set; } = "Palestra"; // Palestra, Minicurso, Congresso, Workshop, etc.

    [Required]
    [MaxLength(30)]
    public string Modality { get; set; } = "Presencial"; // Presencial, Online, Híbrido

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [MaxLength(50)]
    public string Workload { get; set; } = "4 horas";

    [Required]
    [MaxLength(250)]
    public string Location { get; set; } = "IFCE Campus Cedro";

    public int TotalSlots { get; set; } = 50;

    [MaxLength(30)]
    public string Status { get; set; } = "Aberto"; // Aberto, Esgotado, Encerrado

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? OrganizerId { get; set; }

    [ForeignKey(nameof(OrganizerId))]
    public User? Organizer { get; set; }

    // Navigation properties
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
}
