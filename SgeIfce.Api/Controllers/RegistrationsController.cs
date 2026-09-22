using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SgeIfce.Api.Data;
using SgeIfce.Api.DTOs;
using SgeIfce.Api.Models;

namespace SgeIfce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RegistrationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public RegistrationsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Authorize(Roles = "Aluno")]
    public async Task<ActionResult<ApiResponse<RegistrationResponseDto>>> Register([FromBody] CreateRegistrationDto dto)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(ApiResponse<RegistrationResponseDto>.Fail("Usuário não autenticado."));
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return Unauthorized(ApiResponse<RegistrationResponseDto>.Fail("Usuário não autenticado."));
        }

        var ev = await _context.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == dto.EventId);

        if (ev == null)
        {
            return NotFound(ApiResponse<RegistrationResponseDto>.Fail("Evento não encontrado."));
        }

        if (ev.Status == "Encerrado")
        {
            return BadRequest(ApiResponse<RegistrationResponseDto>.Fail("Este evento já foi encerrado."));
        }

        var existingReg = ev.Registrations.FirstOrDefault(r => r.UserId == userId && r.Status == "confirmado");
        if (existingReg != null)
        {
            return Conflict(ApiResponse<RegistrationResponseDto>.Fail(
                "Inscrição já realizada.",
                "Você já possui uma inscrição ativa para este evento."
            ));
        }

        var enrolledCount = ev.Registrations.Count(r => r.Status == "confirmado");
        if (enrolledCount >= ev.TotalSlots)
        {
            return BadRequest(ApiResponse<RegistrationResponseDto>.Fail(
                "Evento esgotado.",
                "Todas as vagas para este evento já foram preenchidas."
            ));
        }

        var ticketCode = $"SGE-{ev.Category.Substring(0, Math.Min(3, ev.Category.Length)).ToUpper()}-{Random.Shared.Next(10000, 99999)}";

        var reg = new Registration
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            EventId = ev.Id,
            EventTitle = ev.Title,
            RegistrationDate = DateTime.UtcNow,
            TicketCode = ticketCode,
            Status = "confirmado",
            CreatedAt = DateTime.UtcNow
        };

        // Automatic attendance record creation
        var attendance = new Attendance
        {
            Id = Guid.NewGuid().ToString(),
            EventId = ev.Id,
            UserId = userId,
            ParticipantName = user.Name,
            ParticipantEmail = user.Email,
            Matricula = user.Matricula ?? string.Empty,
            Status = "pendente",
            CertificateIssued = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Registrations.Add(reg);
        _context.Attendances.Add(attendance);
        await _context.SaveChangesAsync();

        var response = new RegistrationResponseDto
        {
            Id = reg.Id,
            EventId = ev.Id,
            EventTitle = ev.Title,
            Date = ev.StartDate.ToString("dd/MM/yyyy"),
            Status = reg.Status,
            TicketCode = reg.TicketCode,
            Location = ev.Location,
            Modality = ev.Modality,
            Workload = ev.Workload
        };

        return StatusCode(201, ApiResponse<RegistrationResponseDto>.Ok(response, "Inscrição confirmada com sucesso! Código gerado."));
    }

    [HttpGet("my")]
    [Authorize(Roles = "Aluno")]
    public async Task<ActionResult<ApiResponse<List<RegistrationResponseDto>>>> GetMyRegistrations()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(ApiResponse<List<RegistrationResponseDto>>.Fail("Usuário não autenticado."));
        }

        var registrations = await _context.Registrations
            .Include(r => r.Event)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

        var result = registrations.Select(r => new RegistrationResponseDto
        {
            Id = r.Id,
            EventId = r.EventId,
            EventTitle = r.Event?.Title ?? "Evento SGE",
            Date = r.Event?.StartDate.ToString("dd/MM/yyyy") ?? r.CreatedAt.ToString("dd/MM/yyyy"),
            Status = r.Status,
            TicketCode = r.TicketCode,
            Location = r.Event?.Location,
            Modality = r.Event?.Modality,
            Workload = r.Event?.Workload
        }).ToList();

        return Ok(ApiResponse<List<RegistrationResponseDto>>.Ok(result));
    }

    [HttpGet("{id}/ticket")]
    public async Task<ActionResult<ApiResponse<TicketDto>>> GetTicket(string id)
    {
        var reg = await _context.Registrations
            .Include(r => r.Event)
            .Include(r => r.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reg == null)
        {
            return NotFound(ApiResponse<TicketDto>.Fail("Inscrição/Ticket não encontrado."));
        }

        var ev = reg.Event;
        var user = reg.User;

        var ticketDto = new TicketDto
        {
            Id = reg.Id.ToString(),
            Organizer = "SGE-IFCE",
            Year = (ev?.StartDate ?? DateTime.UtcNow).Year.ToString(),
            EventTitle = ev?.Title ?? "Evento IFCE",
            EventDescription = ev?.Description,
            Date = ev?.StartDate.ToString("dd 'de' MMMM") ?? DateTime.UtcNow.ToString("dd 'de' MMMM"),
            Time = ev?.StartDate.ToString("HH:mm") ?? "08:00",
            Location = ev?.Location ?? "IFCE Campus Cedro",
            StatusLabel = reg.Status == "confirmado" ? "Inscrição confirmada" : "Inscrição cancelada",
            TicketNumber = reg.TicketCode,
            ParticipantName = user?.Name ?? "Discente",
            ParticipantMatricula = user?.Matricula ?? ""
        };

        return Ok(ApiResponse<TicketDto>.Ok(ticketDto));
    }

    [HttpPatch("{id}/cancel")]
    [Authorize(Roles = "Aluno")]
    public async Task<ActionResult<ApiResponse<RegistrationResponseDto>>> CancelRegistration(string id)
    {
        var userId = GetCurrentUserId();
        var reg = await _context.Registrations
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (reg == null)
        {
            return NotFound(ApiResponse<RegistrationResponseDto>.Fail("Inscrição não encontrada."));
        }

        if (reg.Status == "cancelado")
        {
            return BadRequest(ApiResponse<RegistrationResponseDto>.Fail("Esta inscrição já se encontra cancelada."));
        }

        reg.Status = "cancelado";

        // Atualiza a presença correspondente se houver
        var attendance = await _context.Attendances.FirstOrDefaultAsync(a =>
            a.EventId == reg.EventId && a.UserId == reg.UserId);
        if (attendance != null)
        {
            attendance.Status = "ausente";
        }

        await _context.SaveChangesAsync();

        var response = new RegistrationResponseDto
        {
            Id = reg.Id,
            EventId = reg.EventId,
            EventTitle = reg.Event?.Title ?? "",
            Date = reg.Event?.StartDate.ToString("dd/MM/yyyy") ?? "",
            Status = reg.Status,
            TicketCode = reg.TicketCode
        };

        return Ok(ApiResponse<RegistrationResponseDto>.Ok(response, "Inscrição cancelada com sucesso."));
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
    }
}
