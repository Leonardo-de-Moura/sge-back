using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SgeIfce.Api.Models;

public class Registration
{
    [Key]
    [MaxLength(64)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Required]
    [MaxLength(64)]
    public string EventId { get; set; } = string.Empty;

    [ForeignKey(nameof(EventId))]
    public Event? Event { get; set; }

    [Required]
    [MaxLength(255)]
    public string EventTitle { get; set; } = string.Empty;

    public DateTime RegistrationDate { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "confirmado";

    [Required]
    [MaxLength(60)]
    public string TicketCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

}