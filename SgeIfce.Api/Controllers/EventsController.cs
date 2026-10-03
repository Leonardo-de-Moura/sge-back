using System.Security.Claims;
using System.Data.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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

    [HttpGet("{eventId}/qrcode")]
    [Authorize(Roles = "Professor")]
    public async Task<ActionResult<ApiResponse<EventQrCodeResponseDto>>> GetEventQrCode(string eventId)
    {
        var ev = await _context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (ev == null)
        {
            return NotFound(ApiResponse<EventQrCodeResponseDto>.Fail("Evento não encontrado."));
        }

        if (string.IsNullOrWhiteSpace(ev.QrCodeToken) || !ev.QrCodeExpiresAt.HasValue || ev.QrCodeExpiresAt <= DateTime.UtcNow)
        {
            return NotFound(ApiResponse<EventQrCodeResponseDto>.Fail("Nenhum QR Code ativo foi gerado para este evento."));
        }

        return Ok(ApiResponse<EventQrCodeResponseDto>.Ok(new EventQrCodeResponseDto
        {
            EventId = ev.Id,
            EventTitle = ev.Title,
            Token = ev.QrCodeToken,
            GeneratedAt = ev.QrCodeGeneratedAt ?? DateTime.UtcNow,
            ExpiresAt = ev.QrCodeExpiresAt.Value,
            IsActive = true
        }, "QR Code ativo encontrado."));
    }

    [HttpPost("{eventId}/qrcode")]
    [Authorize(Roles = "Professor")]
    public async Task<ActionResult<ApiResponse<EventQrCodeResponseDto>>> GenerateEventQrCode(string eventId, [FromBody] GenerateQrCodeRequestDto? dto)
    {
        var ev = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (ev == null)
        {
            return NotFound(ApiResponse<EventQrCodeResponseDto>.Fail("Evento não encontrado."));
        }

        var forceGenerate = dto?.ForceGenerate == true;
        var shouldRegenerate = forceGenerate ||
            string.IsNullOrWhiteSpace(ev.QrCodeToken) ||
            !ev.QrCodeExpiresAt.HasValue ||
            ev.QrCodeExpiresAt <= DateTime.UtcNow;

        if (shouldRegenerate)
        {
            ev.QrCodeToken = CreateSecureToken();
            ev.QrCodeGeneratedAt = DateTime.UtcNow;
            ev.QrCodeExpiresAt = DateTime.UtcNow.AddHours(12);
            await _context.SaveChangesAsync();
        }

        var response = new EventQrCodeResponseDto
        {
            EventId = ev.Id,
            EventTitle = ev.Title,
            Token = ev.QrCodeToken ?? string.Empty,
            GeneratedAt = ev.QrCodeGeneratedAt ?? DateTime.UtcNow,
            ExpiresAt = ev.QrCodeExpiresAt ?? DateTime.UtcNow.AddHours(12),
            IsActive = true
        };

        return Ok(ApiResponse<EventQrCodeResponseDto>.Ok(response, "QR Code atualizado com sucesso."));
    }

    [HttpGet("{eventId}/attendance")]
    [Authorize(Roles = "Professor")]
    public async Task<ActionResult<ApiResponse<List<ParticipantAttendanceDto>>>> GetEventAttendance(string eventId)
    {
        var eventExists = await _context.Events.AnyAsync(e => e.Id == eventId);
        if (!eventExists)
        {
            return NotFound(ApiResponse<List<ParticipantAttendanceDto>>.Fail("Evento não encontrado."));
        }

        var attendances = await _context.Attendances
            .Include(a => a.User)
            .Where(a => a.EventId == eventId)
            .AsNoTracking()
            .ToListAsync();

        var result = attendances.Select(a => new ParticipantAttendanceDto
        {
            Id = a.Id,
            UserId = a.UserId,
            Name = a.User?.Name ?? a.ParticipantName,
            Email = a.User?.Email ?? a.ParticipantEmail,
            Matricula = a.User?.Matricula ?? a.Matricula,
            Status = a.Status,
            CertificateIssued = a.CertificateIssued,
            AvatarUrl = a.User?.AvatarUrl,
            CheckedInAt = a.CheckedInAt
        }).ToList();

        return Ok(ApiResponse<List<ParticipantAttendanceDto>>.Ok(result));
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

        if (!dto.EndDate.HasValue)
        {
            return BadRequest(ApiResponse<EventResponseDto>.Fail(
                "A data e o horário de término do evento são obrigatórios."
            ));
        }

        var startDate = NormalizeUtc(dto.StartDate);
        var endDate = NormalizeUtc(dto.EndDate.Value);
        if (endDate <= startDate)
        {
            return BadRequest(ApiResponse<EventResponseDto>.Fail(
                "A data e o horário de início devem ser anteriores ao término."
            ));
        }

        var location = dto.Location.Trim();
        await using var scheduleLock = await AcquireLocationScheduleLockAsync(location);
        var conflict = await FindScheduleConflictAsync(location, startDate, endDate);
        if (conflict != null)
        {
            return Conflict(CreateScheduleConflictResponse(conflict));
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
            StartDate = startDate,
            EndDate = endDate,
            DayMonth = string.IsNullOrWhiteSpace(dto.DayMonth)
                ? dto.StartDate.ToString("dd/MM")
                : dto.DayMonth.Trim(),
            Workload = string.IsNullOrWhiteSpace(dto.Workload)
                ? "4 horas"
                : dto.Workload.Trim(),
            Location = location,
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

        var effectiveStartDate = NormalizeUtc(dto.StartDate ?? ev.StartDate);
        var effectiveEndDate = dto.EndDate.HasValue
            ? NormalizeUtc(dto.EndDate.Value)
            : ev.EndDate;
        var effectiveLocation = string.IsNullOrWhiteSpace(dto.Location)
            ? ev.Location
            : dto.Location.Trim();
        var scheduleChanged = dto.StartDate.HasValue ||
            dto.EndDate.HasValue ||
            !string.IsNullOrWhiteSpace(dto.Location);

        if (effectiveEndDate.HasValue && effectiveEndDate.Value <= effectiveStartDate)
        {
            return BadRequest(ApiResponse<EventResponseDto>.Fail(
                "A data e o horário de início devem ser anteriores ao término."
            ));
        }

        if (!effectiveEndDate.HasValue && scheduleChanged)
        {
            return BadRequest(ApiResponse<EventResponseDto>.Fail(
                "A data e o horário de término são obrigatórios para alterar a agenda."
            ));
        }

        await using var scheduleLock = effectiveEndDate.HasValue
            ? await AcquireLocationScheduleLockAsync(effectiveLocation)
            : null;
        if (effectiveEndDate.HasValue)
        {
            var conflict = await FindScheduleConflictAsync(
                effectiveLocation,
                effectiveStartDate,
                effectiveEndDate.Value,
                ev.Id
            );

            if (conflict != null)
            {
                return Conflict(CreateScheduleConflictResponse(conflict));
            }
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
            ev.StartDate = effectiveStartDate;

        if (dto.EndDate.HasValue)
            ev.EndDate = effectiveEndDate;

        if (!string.IsNullOrWhiteSpace(dto.Workload))
            ev.Workload = dto.Workload.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Location))
            ev.Location = effectiveLocation;

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

    private async Task<Event?> FindScheduleConflictAsync(
        string location,
        DateTime startDate,
        DateTime endDate,
        string? excludedEventId = null)
    {
        var normalizedLocation = NormalizeLocation(location);

        return await _context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e =>
                e.Id != excludedEventId &&
                e.EndDate.HasValue &&
                e.Location.Trim().ToLower() == normalizedLocation &&
                startDate < e.EndDate.Value &&
                endDate > e.StartDate);
    }

    private static string NormalizeLocation(string location) =>
        location.Trim().ToLowerInvariant();

    private async Task<LocationScheduleLock?> AcquireLocationScheduleLockAsync(string location)
    {
        if (_context.Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            return null;
        }

        await _context.Database.OpenConnectionAsync();
        var connection = _context.Database.GetDbConnection();
        var lockKey = $"sge-event-location:{NormalizeLocation(location)}";

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT pg_advisory_lock(hashtextextended(@location, 0))";
            AddLocationParameter(command, lockKey);
            await command.ExecuteScalarAsync();

            return new LocationScheduleLock(_context.Database, connection, lockKey);
        }
        catch
        {
            await _context.Database.CloseConnectionAsync();
            throw;
        }
    }

    private static void AddLocationParameter(DbCommand command, string lockKey)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = "location";
        parameter.Value = lockKey;
        command.Parameters.Add(parameter);
    }

    private static ApiResponse<EventResponseDto> CreateScheduleConflictResponse(Event conflict)
    {
        var conflictEnd = conflict.EndDate!.Value;
        var interval = conflict.StartDate.Date == conflictEnd.Date
            ? $"{conflict.StartDate:HH:mm} às {conflictEnd:HH:mm}"
            : $"{conflict.StartDate:dd/MM/yyyy HH:mm} às {conflictEnd:dd/MM/yyyy HH:mm}";

        return ApiResponse<EventResponseDto>.Fail(
            "O local já está reservado para outro evento nesse período.",
            $"Conflito de horário no local {conflict.Location.Trim()}: {interval}."
        );
    }

    private sealed class LocationScheduleLock : IAsyncDisposable
    {
        private readonly DatabaseFacade _database;
        private readonly DbConnection _connection;
        private readonly string _lockKey;

        public LocationScheduleLock(DatabaseFacade database, DbConnection connection, string lockKey)
        {
            _database = database;
            _connection = connection;
            _lockKey = lockKey;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await using var command = _connection.CreateCommand();
                command.CommandText = "SELECT pg_advisory_unlock(hashtextextended(@location, 0))";
                AddLocationParameter(command, _lockKey);
                await command.ExecuteScalarAsync();
            }
            finally
            {
                await _database.CloseConnectionAsync();
            }
        }
    }

    private static string CreateSecureToken()
    {
        return Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
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