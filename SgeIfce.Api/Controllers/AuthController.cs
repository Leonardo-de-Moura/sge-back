using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SgeIfce.Api.Data;
using SgeIfce.Api.DTOs;
using SgeIfce.Api.Models;
using SgeIfce.Api.Services;
using SgeIfce.Api;

namespace SgeIfce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly IEmailConfirmationService _emailConfirmationService;
    private readonly IPasswordResetService _passwordResetService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AppDbContext context,
        ITokenService tokenService,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        IEmailConfirmationService emailConfirmationService,
        IPasswordResetService passwordResetService,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _emailConfirmationService = emailConfirmationService;
        _passwordResetService = passwordResetService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(
        [FromBody] LoginRequestDto dto,
        CancellationToken cancellationToken = default)
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
                u => u.Email.ToLower() == normalizedEmail,
                cancellationToken
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

        if (!user.EmailConfirmed)
        {
            return Unauthorized(
                ApiResponse<LoginResponseDto>.Fail(
                    "E-mail não confirmado.",
                    "Confirme seu endereço de e-mail antes de fazer login."
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
    [EnableRateLimiting(RateLimitingSetup.RegisterPolicy)]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> RegisterStudent(
        [FromBody] RegisterStudentDto dto,
        CancellationToken cancellationToken = default)
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
                u => u.Email.ToLower() == normalizedEmail,
                cancellationToken))
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

        await _context.SaveChangesAsync(cancellationToken);

        var emailSent = await TrySendConfirmationEmailAsync(user, cancellationToken);

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
                emailSent
                    ? "Conta de discente criada com sucesso."
                    : "Conta criada, mas o e-mail de confirmação não pôde ser enviado. Use a opção de reenvio para solicitar um novo link."
            )
        );
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> ConfirmEmail(
        [FromBody] ConfirmEmailDto dto,
        CancellationToken cancellationToken = default)
    {
        var token = await _emailConfirmationService.FindTokenAsync(
            dto.Token,
            cancellationToken);
        var now = DateTime.UtcNow;

        if (token == null)
        {
            return BadRequest(
                ApiResponse<object>.Fail(
                    "Link de confirmação inválido ou expirado.",
                    "Solicite um novo e-mail de confirmação."
                )
            );
        }

        if (token.UsedAt.HasValue &&
            token.User.EmailConfirmed &&
            token.User.EmailConfirmedAt.HasValue &&
            token.UsedAt.Value == token.User.EmailConfirmedAt.Value)
        {
            return Ok(
                ApiResponse<object>.Ok(null, "E-mail já confirmado.")
            );
        }

        if (token.UsedAt.HasValue || token.ExpiresAt <= now)
        {
            return BadRequest(
                ApiResponse<object>.Fail(
                    "Link de confirmação inválido ou expirado.",
                    "Solicite um novo e-mail de confirmação."
                )
            );
        }

        if (token.User.EmailConfirmed)
        {
            return Ok(
                ApiResponse<object>.Ok(null, "E-mail já confirmado.")
            );
        }

        token.User.EmailConfirmed = true;
        token.User.EmailConfirmedAt = now;
        token.UsedAt = now;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(
            ApiResponse<object>.Ok(null, "E-mail confirmado com sucesso.")
        );
    }

    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingSetup.ResendConfirmationPolicy)]
    public async Task<ActionResult<ApiResponse<object>>> ResendConfirmation(
        [FromBody] ResendConfirmationDto dto,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(
            existingUser => existingUser.Email.ToLower() == normalizedEmail,
            cancellationToken);

        if (user is { EmailConfirmed: false })
        {
            var hasRecentToken = await _emailConfirmationService.HasRecentTokenAsync(
                user.Id,
                cancellationToken);
            if (!hasRecentToken)
            {
                await TrySendConfirmationEmailAsync(user, cancellationToken);
            }
        }

        return Ok(
            ApiResponse<object>.Ok(
                null,
                "Se o e-mail estiver cadastrado e ainda não confirmado, enviaremos um novo link."
            )
        );
    }

    [HttpPost("register/teacher")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<UserProfileDto>> RegisterTeacher(
        [FromBody] RegisterTeacherDto dto)
    {
        return StatusCode(
            StatusCodes.Status403Forbidden,
            ApiResponse<UserProfileDto>.Fail(
                "Cadastro de docente indisponível.",
                "A criação de contas de professor requer aprovação administrativa."
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
    [EnableRateLimiting(RateLimitingSetup.ForgotPasswordPolicy)]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword(
        [FromBody] ForgotPasswordDto dto,
        CancellationToken cancellationToken = default)
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
            .FirstOrDefaultAsync(
                u => u.Email.ToLower() == normalizedEmail.ToLower(),
                cancellationToken);

        if (user != null)
        {
            var token = await _passwordResetService.CreateTokenAsync(user);
            var resetLink = BuildFrontendLink("/reset-password?token=", token);

            var resetHtml = BuildEmailHtml(
                title: "Recuperação de senha",
                intro: "Você solicitou a recuperação da sua senha no SGE.IFCE.",
                description: "Use o link abaixo para criar uma nova senha. Ele é válido por 30 minutos.",
                buttonText: "Redefinir senha",
                link: resetLink,
                footer: "Se você não solicitou essa alteração, ignore este e-mail.");

            try
            {
                await _emailService.SendAsync(
                    user.Email,
                    "Recuperação de senha - SGE.IFCE",
                    resetHtml,
                    cancellationToken);
            }
            catch (Exception)
            {
                _logger.LogError(
                    "Falha ao enviar e-mail de recuperação para UserId {UserId}.",
                    user.Id);
            }
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
        [FromBody] ResetPasswordDto dto,
        CancellationToken cancellationToken = default)
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
        await _context.SaveChangesAsync(cancellationToken);

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

    private string BuildFrontendLink(string pathAndQuery, string token)
    {
        var frontendUrl = _configuration["FrontendUrl"];
        if (string.IsNullOrWhiteSpace(frontendUrl))
        {
            throw new InvalidOperationException("FrontendUrl precisa estar configurada.");
        }

        return $"{frontendUrl.TrimEnd('/')}{pathAndQuery}{Uri.EscapeDataString(token)}";
    }

    private async Task<bool> TrySendConfirmationEmailAsync(
        User user,
        CancellationToken cancellationToken)
    {
        try
        {
            var token = await _emailConfirmationService.CreateTokenAsync(user, cancellationToken);
            var confirmationLink = BuildFrontendLink("/confirmar-email?token=", token);
            var confirmationHtml = BuildEmailHtml(
                title: "Confirme seu cadastro",
                intro: $"Olá, {user.Name}. Seu cadastro no SGE.IFCE foi realizado.",
                description: "Confirme seu endereço de e-mail para ativar sua conta. Este link é válido por 24 horas.",
                buttonText: "Confirmar e-mail",
                link: confirmationLink,
                footer: "Se você não criou esta conta, ignore esta mensagem.");

            await _emailService.SendAsync(
                user.Email,
                "Confirmação de cadastro - SGE.IFCE",
                confirmationHtml,
                cancellationToken);
            return true;
        }
        catch (Exception)
        {
            _logger.LogError(
                "Falha ao enviar e-mail de confirmação para UserId {UserId}.",
                user.Id);
            return false;
        }
    }

    private static string BuildEmailHtml(
        string title,
        string intro,
        string description,
        string buttonText,
        string link,
        string footer)
    {
        var encodedTitle = HtmlEncoder.Default.Encode(title);
        var encodedIntro = HtmlEncoder.Default.Encode(intro);
        var encodedDescription = HtmlEncoder.Default.Encode(description);
        var encodedButtonText = HtmlEncoder.Default.Encode(buttonText);
        var encodedLink = HtmlEncoder.Default.Encode(link);
        var encodedFooter = HtmlEncoder.Default.Encode(footer);

        return $"""
            <!doctype html>
            <html lang="pt-BR">
              <body style="margin:0;padding:24px;background:#f5f7f8;font-family:Arial,Helvetica,sans-serif;color:#1f2937;">
                <main style="max-width:600px;margin:0 auto;padding:32px;background:#fff;border-radius:12px;">
                  <h1 style="color:#006a38;font-size:24px;">SGE.IFCE</h1>
                  <h2>{encodedTitle}</h2>
                  <p>{encodedIntro}</p>
                  <p>{encodedDescription}</p>
                  <p><a href="{encodedLink}" style="display:inline-block;padding:12px 20px;background:#006a38;color:#fff;text-decoration:none;border-radius:6px;">{encodedButtonText}</a></p>
                  <p style="font-size:13px;word-break:break-all;">Se o botão não funcionar, copie este link: {encodedLink}</p>
                  <p style="font-size:12px;color:#4b5563;">{encodedFooter}</p>
                </main>
              </body>
            </html>
            """;
    }
}