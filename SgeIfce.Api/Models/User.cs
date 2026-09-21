using System.ComponentModel.DataAnnotations;

namespace SgeIfce.Api.Models;

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Role { get; set; } = "Aluno"; // "Aluno" | "Professor"

    [MaxLength(50)]
    public string? Matricula { get; set; }

    [MaxLength(50)]
    public string? Siape { get; set; }

    [MaxLength(300)]
    public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
    public ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
}
