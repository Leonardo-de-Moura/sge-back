using System.ComponentModel.DataAnnotations;

namespace SgeIfce.Api.Models;

public class User
{
    [Key]
    [MaxLength(64)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = "Aluno";

    [MaxLength(50)]
    public string? Matricula { get; set; }

    [MaxLength(50)]
    public string? Siape { get; set; }

    [MaxLength(100)]
    public string? Department { get; set; }

    [MaxLength(100)]
    public string? Course { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();

    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();

    public ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
}