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
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthController(
        AppDbContext context,
        ITokenService tokenService,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(
        [FromBody] LoginRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(
                ApiResponse<LoginResponseDto>.Fail(
                    "Dados de login inválidos.",
                    errors
                )
            );
        }

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(
                u => u.Email.ToLower() == normalizedEmail
            );

        if (user == null ||
            !_passwordHasher.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(
                ApiResponse<LoginResponseDto>.Fail(
                    "Credenciais inválidas.",
                    "E-mail ou senha incorretos."
                )
            );
        }

        var (token, expiresAt) = _tokenService.GenerateToken(user);

        var profile = new UserProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            Matricula = user.Matricula,
            Siape = user.Siape,
            AvatarUrl = user.AvatarUrl
        };

        var responseData = new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = profile
        };

        return Ok(
            ApiResponse<LoginResponseDto>.Ok(
                responseData,
                "Login realizado com sucesso."
            )
        );
    }

    [HttpPost("register/student")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> RegisterStudent(
        [FromBody] RegisterStudentDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(
                ApiResponse<UserProfileDto>.Fail(
                    "Dados de cadastro inválidos.",
                    errors
                )
            );
        }

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

        if (await _context.Users.AnyAsync(
                u => u.Email.ToLower() == normalizedEmail))
        {
            return Conflict(
                ApiResponse<UserProfileDto>.Fail(
                    "E-mail já cadastrado.",
                    "O e-mail informado já está em uso por outro usuário."
                )
            );
        }

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name.Trim(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            Role = "Aluno",
            Matricula = dto.Matricula.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        var profile = new UserProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            Matricula = user.Matricula
        };

        return StatusCode(
            201,
            ApiResponse<UserProfileDto>.Ok(
                profile,
                "Conta de discente criada com sucesso."
            )
        );
    }

    [HttpPost("register/teacher")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> RegisterTeacher(
        [FromBody] RegisterTeacherDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(
                ApiResponse<UserProfileDto>.Fail(
                    "Dados de cadastro inválidos.",
                    errors
                )
            );
        }

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

        if (await _context.Users.AnyAsync(
                u => u.Email.ToLower() == normalizedEmail))
        {
            return Conflict(
                ApiResponse<UserProfileDto>.Fail(
                    "E-mail já cadastrado.",
                    "O e-mail informado já está em uso por outro usuário."
                )
            );
        }

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name.Trim(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            Role = "Professor",
            Siape = dto.Siape.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        var profile = new UserProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            Siape = user.Siape
        };

        return StatusCode(
            201,
            ApiResponse<UserProfileDto>.Ok(
                profile,
                "Conta de docente criada com sucesso."
            )
        );
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(
                ApiResponse<UserProfileDto>.Fail(
                    "Não autorizado.",
                    "Token inválido."
                )
            );
        }

        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            return NotFound(
                ApiResponse<UserProfileDto>.Fail(
                    "Usuário não encontrado."
                )
            );
        }

        var profile = new UserProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            Matricula = user.Matricula,
            Siape = user.Siape,
            AvatarUrl = user.AvatarUrl
        };

        return Ok(
            ApiResponse<UserProfileDto>.Ok(profile)
        );
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<object>> ForgotPassword(
        [FromBody] ForgotPasswordDto dto)
    {
        return Ok(
            ApiResponse<object>.Ok(
                null,
                "Se o e-mail informado estiver cadastrado, as instruções de recuperação foram enviadas."
            )
        );
    }
}