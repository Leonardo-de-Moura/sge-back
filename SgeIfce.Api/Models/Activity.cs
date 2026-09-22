using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SgeIfce.Api.Models;

public class Activity
{
    [Key]
    [MaxLength(64)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string EventId { get; set; } = string.Empty;

    [ForeignKey(nameof(EventId))]
    public Event? Event { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(60)]
    public string Time { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Speaker { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    public int Order { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}