using System.ComponentModel.DataAnnotations;

namespace SgeIfce.Api.DTOs;

public class CreateRegistrationDto
{
    [Required(ErrorMessage = "O ID do evento é obrigatório.")]
    public string EventId { get; set; } = string.Empty;
}

public class RegistrationResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TicketCode { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Modality { get; set; }
    public string? Workload { get; set; }
}

public class TicketDto
{
    public string Id { get; set; } = string.Empty;
    public string Organizer { get; set; } = "SGE-IFCE";
    public string Year { get; set; } = DateTime.UtcNow.Year.ToString();
    public string EventTitle { get; set; } = string.Empty;
    public string? EventDescription { get; set; }
    public string Date { get; set; } = string.Empty;
    public string? Time { get; set; }
    public string? Location { get; set; }
    public string StatusLabel { get; set; } = "Inscrição confirmada";
    public string TicketNumber { get; set; } = "0000";
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantMatricula { get; set; } = string.Empty;
}
