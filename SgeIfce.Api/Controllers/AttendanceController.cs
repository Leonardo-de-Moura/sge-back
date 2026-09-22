using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SgeIfce.Api.Data;
using SgeIfce.Api.DTOs;

namespace SgeIfce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Professor")]
public class AttendanceController : ControllerBase
{
    private readonly AppDbContext _context;

    public AttendanceController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("event/{eventId}")]
    public async Task<ActionResult<ApiResponse<List<ParticipantAttendanceDto>>>> GetAttendancesByEvent(string eventId)
    {
        var attendances = await _context.Attendances
            .Include(a => a.User)
            .Where(a => a.EventId == eventId)
            .AsNoTracking()
            .ToListAsync();

        var result = attendances.Select(a => new ParticipantAttendanceDto
        {
            Id = a.Id,
            UserId = a.UserId,
            Name = a.User?.Name ?? "Discente",
            Email = a.User?.Email ?? "",
            Matricula = a.User?.Matricula ?? a.User?.Siape ?? "N/A",
            Status = a.Status,
            CertificateIssued = a.CertificateIssued,
            AvatarUrl = a.User?.AvatarUrl,
            CheckedInAt = a.CheckedInAt
        }).ToList();

        return Ok(ApiResponse<List<ParticipantAttendanceDto>>.Ok(result));
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<ApiResponse<ParticipantAttendanceDto>>> UpdateStatus(string id, [FromBody] UpdateAttendanceDto dto)
    {
        var attendance = await _context.Attendances
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attendance == null)
        {
            return NotFound(ApiResponse<ParticipantAttendanceDto>.Fail("Registro de presença não encontrado."));
        }

        var normalizedStatus = dto.Status.Trim().ToLowerInvariant();
        if (normalizedStatus != "presente" && normalizedStatus != "ausente" && normalizedStatus != "pendente")
        {
            return BadRequest(ApiResponse<ParticipantAttendanceDto>.Fail("Status inválido. Use 'presente', 'ausente' ou 'pendente'."));
        }

        attendance.Status = normalizedStatus;
        if (normalizedStatus == "presente" && !attendance.CheckedInAt.HasValue)
        {
            attendance.CheckedInAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        var result = new ParticipantAttendanceDto
        {
            Id = attendance.Id,
            UserId = attendance.UserId,
            Name = attendance.User?.Name ?? "",
            Email = attendance.User?.Email ?? "",
            Matricula = attendance.User?.Matricula ?? "N/A",
            Status = attendance.Status,
            CertificateIssued = attendance.CertificateIssued,
            CheckedInAt = attendance.CheckedInAt
        };

        return Ok(ApiResponse<ParticipantAttendanceDto>.Ok(result, "Status de presença atualizado."));
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<ApiResponse<object>>> BulkUpdate([FromBody] BulkAttendanceDto dto)
    {
        if (dto.Attendances == null || !dto.Attendances.Any())
        {
            return BadRequest(ApiResponse<object>.Fail("Lista de presenças vazia."));
        }

        var ids = dto.Attendances.Select(x => x.Id).ToList();
        var records = await _context.Attendances.Where(a => ids.Contains(a.Id)).ToListAsync();

        foreach (var item in dto.Attendances)
        {
            var record = records.FirstOrDefault(r => r.Id == item.Id);
            if (record != null)
            {
                var norm = item.Status.Trim().ToLowerInvariant();
                if (norm == "presente" || norm == "ausente" || norm == "pendente")
                {
                    record.Status = norm;
                    if (norm == "presente" && !record.CheckedInAt.HasValue)
                    {
                        record.CheckedInAt = DateTime.UtcNow;
                    }
                }
            }
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(null, "Presenças salvas com sucesso!"));
    }
}
