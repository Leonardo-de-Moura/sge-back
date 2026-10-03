using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SgeIfce.Api.Data;
using SgeIfce.Api.DTOs;
using SgeIfce.Api.Models;
using SgeIfce.Api.Services;

namespace SgeIfce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CertificatesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICertificatePdfService _pdfService;

    public CertificatesController(AppDbContext context, ICertificatePdfService pdfService)
    {
        _context = context;
        _pdfService = pdfService;
    }

    [HttpGet("my")]
    [Authorize(Roles = "Aluno")]
    public async Task<ActionResult<ApiResponse<List<CertificateResponseDto>>>> GetMyCertificates()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            return Unauthorized(ApiResponse<List<CertificateResponseDto>>.Fail("Usuário não autenticado."));
        }

        var certificates = await _context.Certificates
            .Include(c => c.Event)
            .Where(c => c.UserId == userIdClaim)
            .OrderByDescending(c => c.IssueDate)
            .AsNoTracking()
            .ToListAsync();

        var result = certificates.Select(c => new CertificateResponseDto
        {
            Id = c.Id,
            EventId = c.EventId,
            EventTitle = c.EventTitle,
            IssueDate = c.IssueDate.ToString("dd/MM/yyyy"),
            Workload = c.Workload,
            ValidationCode = c.ValidationCode,
            ParticipantName = c.ParticipantName
        }).ToList();

        return Ok(ApiResponse<List<CertificateResponseDto>>.Ok(result));
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<CertificateDetailDto>>> GetCertificateById(string id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var cert = await _context.Certificates
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cert == null)
        {
            return NotFound(ApiResponse<CertificateDetailDto>.Fail("Certificado não encontrado."));
        }

        if (!User.IsInRole("Professor") && !string.Equals(cert.UserId, userIdClaim, StringComparison.Ordinal))
        {
            return Forbid();
        }

        return Ok(ApiResponse<CertificateDetailDto>.Ok(new CertificateDetailDto
        {
            Id = cert.Id,
            EventId = cert.EventId,
            EventTitle = cert.EventTitle,
            ParticipantName = cert.ParticipantName,
            ParticipantEmail = cert.ParticipantEmail,
            Workload = cert.Workload,
            ValidationCode = cert.ValidationCode,
            IssueDate = cert.IssueDate.ToString("dd/MM/yyyy")
        }, "Certificado consultado com sucesso."));
    }

    [HttpGet("validate/{code}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<ValidateCertificateDto>>> ValidateCertificate(string code)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var cert = await _context.Certificates
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ValidationCode.ToUpper() == normalizedCode);

        if (cert == null)
        {
            return NotFound(ApiResponse<ValidateCertificateDto>.Fail(
                "Certificado não encontrado.",
                "O código verificador fornecido não corresponde a nenhum certificado oficial emitido pelo IFCE."
            ));
        }

        var dto = new ValidateCertificateDto
        {
            Valid = true,
            ValidationCode = cert.ValidationCode,
            ParticipantName = cert.ParticipantName,
            EventTitle = cert.EventTitle,
            Workload = cert.Workload,
            IssueDate = cert.IssueDate.ToString("dd/MM/yyyy"),
            Institution = "Instituto Federal de Educação, Ciência e Tecnologia do Ceará - Campus Cedro"
        };

        return Ok(ApiResponse<ValidateCertificateDto>.Ok(dto, "Certificado autêntico emitido pelo IFCE."));
    }

    [HttpPost("issue")]
    [Authorize(Roles = "Professor")]
    public async Task<ActionResult<ApiResponse<IssueResultDto>>> IssueCertificates([FromBody] IssueCertificateDto dto)
    {
        var ev = await _context.Events.FindAsync(dto.EventId);
        if (ev == null)
        {
            return NotFound(ApiResponse<IssueResultDto>.Fail("Evento não encontrado."));
        }

        var query = _context.Attendances
            .Include(a => a.User)
            .Where(a => a.EventId == dto.EventId && a.Status == "presente" && !a.CertificateIssued);

        if (dto.AttendanceIds != null && dto.AttendanceIds.Any())
        {
            query = query.Where(a => dto.AttendanceIds.Contains(a.Id));
        }

        var eligibleAttendances = await query.ToListAsync();

        if (!eligibleAttendances.Any())
        {
            return Ok(ApiResponse<IssueResultDto>.Ok(
                new IssueResultDto { TotalIssued = 0 },
                "Nenhum participante elegível pendente de certificação."
            ));
        }

        var issuedCodes = new List<string>();

        foreach (var att in eligibleAttendances)
        {
            string code;
            do
            {
                code = $"IFCE-CED-{DateTime.UtcNow.Year}-CERT-{Guid.NewGuid():N}".ToUpperInvariant();
            }
            while (issuedCodes.Contains(code) ||
                   await _context.Certificates.AnyAsync(c => c.ValidationCode == code));

            var certificate = new Certificate
            {
                Id = Guid.NewGuid().ToString(),
                EventId = ev.Id,
                UserId = att.UserId,
                ValidationCode = code,
                EventTitle = ev.Title,
                ParticipantName = att.User?.Name ?? "Participante",
                ParticipantEmail = att.User?.Email ?? att.ParticipantEmail,
                Matricula = att.User?.Matricula ?? att.Matricula,
                Workload = ev.Workload,
                IssueDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            att.CertificateIssued = true;
            _context.Certificates.Add(certificate);
            issuedCodes.Add(code);
        }

        await _context.SaveChangesAsync();

        var result = new IssueResultDto
        {
            TotalIssued = issuedCodes.Count,
            IssuedCodes = issuedCodes
        };

        return Ok(ApiResponse<IssueResultDto>.Ok(result, $"{result.TotalIssued} certificados emitidos com sucesso!"));
    }

    [HttpGet("{id}/pdf")]
    [Authorize]
    public async Task<IActionResult> DownloadCertificatePdf(string id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var cert = await _context.Certificates
            .Include(c => c.Event)
                .ThenInclude(e => e!.Organizer)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (cert == null)
        {
            return NotFound(ApiResponse<object>.Fail("Certificado não encontrado."));
        }

        if (!User.IsInRole("Professor") && !string.Equals(cert.UserId, userIdClaim, StringComparison.Ordinal))
        {
            return Forbid();
        }

        var pdfBytes = _pdfService.GeneratePdf(cert);
        var filename = $"Certificado-IFCE-{cert.ValidationCode}.pdf";

        return File(pdfBytes, "application/pdf", filename);
    }

    [HttpGet("{id}/download")]
    [Authorize]
    public async Task<IActionResult> DownloadCertificatePdfLegacy(string id)
    {
        return await DownloadCertificatePdf(id);
    }
}
