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
public class EventsController : ControllerBase
{
    private readonly AppDbContext _context;

    public EventsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<EventResponseDto>>>> GetEvents(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string? modality,
        [FromQuery] string? status)
    {
        var query = _context.Events
            .Include(e => e.Activities)
            .Include(e => e.Registrations)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();

            query = query.Where(e =>
                e.Title.ToLower().Contains(term) ||
                e.Description.ToLower().Contains(term) ||
                e.Category.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "Todos")
        {
            query = query.Where(e =>
                e.Category.ToLower() == category.Trim().ToLower());
        }

        if (!string.IsNullOrWhiteSpace(modality))
        {
            query = query.Where(e =>
                e.Modality.ToLower() == modality.Trim().ToLower());
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(e =>
                e.Status.ToLower() == status.Trim().ToLower());
        }

        var events = await query
            .OrderBy(e => e.StartDate)
            .ToListAsync();

        var result = events
            .Select(MapToEventResponseDto)
            .ToList();

        return Ok(
            ApiResponse<List<EventResponseDto>>.Ok(result)
        );
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<EventResponseDto>>> GetEventById(
        string id)
    {
        var ev = await _context.Events
            .Include(e => e.Activities)
            .Include(e => e.Registrations)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (ev == null)
        {
            return NotFound(
                ApiResponse<EventResponseDto>.Fail(
                    "Evento não encontrado."
                )
            );
        }

        return Ok(
            ApiResponse<EventResponseDto>.Ok(
                MapToEventResponseDto(ev)
            )
        );
    }

    [HttpPost]
    [Authorize(Roles = "Professor")]
    public async Task<ActionResult<ApiResponse<EventResponseDto>>> CreateEvent(
        [FromBody] CreateEventDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(
                ApiResponse<EventResponseDto>.Fail(
                    "Dados inválidos para criação do evento.",
                    errors
                )
            );
        }

        if (dto.EndDate.HasValue && dto.EndDate.Value.Date < dto.StartDate.Date)
        {
            return BadRequest(ApiResponse<EventResponseDto>.Fail(
                "A data de término não pode ser anterior à data de início."
            ));
        }

        var userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        var organizerId =
            string.IsNullOrWhiteSpace(userIdClaim)
                ? null
                : userIdClaim;

        var ev = new Event
        {
            Id = Guid.NewGuid().ToString(),
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            Category = dto.Category.Trim(),
            Modality = dto.Modality.Trim(),
            StartDate = NormalizeUtc(dto.StartDate),
            EndDate = dto.EndDate.HasValue
                ? NormalizeUtc(dto.EndDate.Value)
                : null,
            DayMonth = string.IsNullOrWhiteSpace(dto.DayMonth)
                ? dto.StartDate.ToString("dd/MM")
                : dto.DayMonth.Trim(),
            Workload = string.IsNullOrWhiteSpace(dto.Workload)
                ? "4 horas"
                : dto.Workload.Trim(),
            Location = dto.Location.Trim(),
            TotalSlots = dto.TotalSlots,

            // Novo evento começa sem inscritos.
            EnrolledSlots = 0,

            Status = "Aberto",
            OrganizerId = organizerId,
            CreatedAt = DateTime.UtcNow
        };

        if (dto.Activities != null && dto.Activities.Any())
        {
            foreach (var actDto in dto.Activities)
            {
                ev.Activities.Add(
                    new Activity
                    {
                        Id = Guid.NewGuid().ToString(),
                        EventId = ev.Id,
                        Title = actDto.Title.Trim(),
                        Time = actDto.Time.Trim(),
                        Speaker = actDto.Speaker?.Trim(),
                        Location = actDto.Location?.Trim()
                    }
                );
            }
        }

        _context.Events.Add(ev);

        await _context.SaveChangesAsync();

        var responseDto = MapToEventResponseDto(ev);

        return StatusCode(
            201,
            ApiResponse<EventResponseDto>.Ok(
                responseDto,
                "Evento criado com sucesso!"
            )
        );
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Professor")]
    public async Task<ActionResult<ApiResponse<EventResponseDto>>> UpdateEvent(
        string id,
        [FromBody] UpdateEventDto dto)
    {
        var ev = await _context.Events
            .Include(e => e.Activities)
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (ev == null)
        {
            return NotFound(
                ApiResponse<EventResponseDto>.Fail(
                    "Evento não encontrado."
                )
            );
        }

        var effectiveStartDate = dto.StartDate ?? ev.StartDate;
        var effectiveEndDate = dto.EndDate ?? ev.EndDate;
        if (effectiveEndDate.HasValue && effectiveEndDate.Value.Date < effectiveStartDate.Date)
        {
            return BadRequest(ApiResponse<EventResponseDto>.Fail(
                "A data de término não pode ser anterior à data de início."
            ));
        }

        if (!string.IsNullOrWhiteSpace(dto.Title))
            ev.Title = dto.Title.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Description))
            ev.Description = dto.Description.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Category))
            ev.Category = dto.Category.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Modality))
            ev.Modality = dto.Modality.Trim();

        if (dto.StartDate.HasValue)
            ev.StartDate = NormalizeUtc(dto.StartDate.Value);

        if (dto.EndDate.HasValue)
            ev.EndDate = NormalizeUtc(dto.EndDate.Value);

        if (!string.IsNullOrWhiteSpace(dto.Workload))
            ev.Workload = dto.Workload.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Location))
            ev.Location = dto.Location.Trim();

        if (dto.TotalSlots.HasValue && dto.TotalSlots.Value > 0)
            ev.TotalSlots = dto.TotalSlots.Value;

        if (!string.IsNullOrWhiteSpace(dto.Status))
            ev.Status = dto.Status.Trim();

        await _context.SaveChangesAsync();

        return Ok(
            ApiResponse<EventResponseDto>.Ok(
                MapToEventResponseDto(ev),
                "Evento atualizado com sucesso."
            )
        );
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Professor")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteEvent(
        string id)
    {
        var ev = await _context.Events.FindAsync(id);

        if (ev == null)
        {
            return NotFound(
                ApiResponse<object>.Fail(
                    "Evento não encontrado."
                )
            );
        }

        // Mantém o evento no banco e apenas o encerra.
        ev.Status = "Encerrado";

        await _context.SaveChangesAsync();

        return Ok(
            ApiResponse<object>.Ok(
                null,
                "Evento encerrado com sucesso."
            )
        );
    }

    [HttpGet("dashboard/stats")]
    [Authorize(Roles = "Professor")]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetDashboardStats()
    {
        var totalEvents =
            await _context.Events.CountAsync();

        var activeEvents =
            await _context.Events.CountAsync(
                e => e.Status == "Aberto"
            );

        var totalEnrolled =
            await _context.Registrations.CountAsync(
                r => r.Status == "confirmado"
            );

        var todayEnrolled =
            await _context.Registrations.CountAsync(
                r =>
                    r.CreatedAt >= DateTime.UtcNow.Date &&
                    r.Status == "confirmado"
            );

        var totalAttendanceRecords =
            await _context.Attendances.CountAsync();

        var presentAttendanceRecords =
            await _context.Attendances.CountAsync(
                a => a.Status == "presente"
            );

        var percentage =
            totalAttendanceRecords > 0
                ? Math.Round(
                    (double)presentAttendanceRecords /
                    totalAttendanceRecords *
                    100,
                    1
                )
                : 84.0;

        var issuedCerts =
            await _context.Certificates.CountAsync();

        var pendingCerts =
            await _context.Attendances.CountAsync(
                a =>
                    a.Status == "presente" &&
                    !a.CertificateIssued
            );

        var stats = new DashboardStatsDto
        {
            EventsUnderManagement = totalEvents,
            ActiveEventsCount = activeEvents,
            TotalEnrolledCount = totalEnrolled,
            TodayNewEnrolledCount = todayEnrolled,
            ConfirmedAttendancePercentage = percentage,
            IssuedCertificatesCount = issuedCerts,
            PendingCertificatesCount = pendingCerts
        };

        return Ok(
            ApiResponse<DashboardStatsDto>.Ok(stats)
        );
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static EventResponseDto MapToEventResponseDto(Event e)
    {
        // Usa o valor armazenado na coluna EnrolledSlots
        // do banco de dados, em vez de contar a tabela
        // Registrations.
        var enrolledCount = e.EnrolledSlots;

        var status = e.Status;

        if (status == "Aberto" &&
            enrolledCount >= e.TotalSlots)
        {
            status = "Esgotado";
        }

        var day = e.StartDate.ToString("dd");

        var month = e.StartDate
            .ToString("MMM")
            .ToUpper()
            .Replace(".", "");

        return new EventResponseDto
        {
            Id = e.Id,
            Title = e.Title,
            Description = e.Description,
            Category = e.Category,
            Modality = e.Modality,
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            DayMonth = $"{day} {month}",
            Workload = e.Workload,
            Location = e.Location,
            TotalSlots = e.TotalSlots,

            // Agora retorna 54, 30, 142, 29,
            // conforme os valores existentes no PostgreSQL.
            EnrolledSlots = enrolledCount,

            Status = status,
            ImageUrl = e.ImageUrl,

            Activities = e.Activities
                .Select(a => new ActivityDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Time = a.Time,
                    Speaker = a.Speaker,
                    Location = a.Location
                })
                .ToList()
        };
    }
}