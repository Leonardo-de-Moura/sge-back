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
    private readonly IEmailService _emailService;
    private readonly IPasswordResetService _passwordResetService;
    private readonly IConfiguration _configuration;

    public AuthController(
        AppDbContext context,
        ITokenService tokenService,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        IPasswordResetService passwordResetService,
        IConfiguration configuration)
    {
        _context = context;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _passwordResetService = passwordResetService;
        _configuration = configuration;
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
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = false
        };

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:3000";
        var confirmationLink = $"{frontendUrl.TrimEnd('/')}/login/aluno?confirmed=1";
        await _emailService.SendAccountConfirmationAsync(user.Email, confirmationLink);

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
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = false
        };

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:3000";
        var confirmationLink = $"{frontendUrl.TrimEnd('/')}/login/professor?confirmed=1";
        await _emailService.SendAccountConfirmationAsync(user.Email, confirmationLink);

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
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword(
        [FromBody] ForgotPasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(
                ApiResponse<object>.Fail(
                    "Dados inválidos.",
                    errors
                )
            );
        }

        var normalizedEmail = dto.Email.Trim();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail.ToLower());

        if (user != null)
        {
            var token = await _passwordResetService.CreateTokenAsync(user);
            var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:3000";
            var resetLink = $"{frontendUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}";

            await _emailService.SendPasswordResetAsync(user.Email, resetLink);
        }

        return Ok(
            ApiResponse<object>.Ok(
                null,
                "Se o e-mail estiver cadastrado, enviaremos instruções para recuperação da senha."
            )
        );
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(
        [FromBody] ResetPasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(
                ApiResponse<object>.Fail(
                    "Dados de redefinição inválidos.",
                    errors
                )
            );
        }

        if (string.IsNullOrWhiteSpace(dto.Token))
        {
            return BadRequest(
                ApiResponse<object>.Fail(
                    "Token inválido.",
                    "O token de redefinição é obrigatório."
                )
            );
        }

        if (!IsPasswordValid(dto.NewPassword))
        {
            return BadRequest(
                ApiResponse<object>.Fail(
                    "Senha inválida.",
                    "A senha deve ter no mínimo 8 caracteres, com letras e números."
                )
            );
        }

        if (dto.NewPassword != dto.ConfirmPassword)
        {
            return BadRequest(
                ApiResponse<object>.Fail(
                    "Confirmação de senha inválida.",
                    "As senhas digitadas não coincidem."
                )
            );
        }

        var (user, token) = await _passwordResetService.ValidateTokenAsync(dto.Token);

        if (user == null || token == null)
        {
            return BadRequest(
                ApiResponse<object>.Fail(
                    "Token inválido ou expirado.",
                    "O link de redefinição não é mais válido."
                )
            );
        }

        if (token.UsedAt.HasValue || token.ExpiresAt <= DateTime.UtcNow)
        {
            return BadRequest(
                ApiResponse<object>.Fail(
                    "Token inválido ou expirado.",
                    "Este token já foi utilizado ou expirou."
                )
            );
        }

        user.PasswordHash = _passwordHasher.Hash(dto.NewPassword);
        token.UsedAt = DateTime.UtcNow;

        await _passwordResetService.InvalidateUserTokensAsync(user.Id);
        await _context.SaveChangesAsync();

        return Ok(
            ApiResponse<object>.Ok(
                null,
                "Senha redefinida com sucesso."
            )
        );
    }

    private static bool IsPasswordValid(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            return false;
        }

        return password.Any(char.IsLetter) && password.Any(char.IsDigit);
    }
}