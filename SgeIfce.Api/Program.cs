using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using SgeIfce.Api.Data;
using SgeIfce.Api.Middleware;
using SgeIfce.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. CONFIGURAÇÃO DO BANCO DE DADOS
// ============================================================
// Produção e desenvolvimento utilizam PostgreSQL.
// A conexão deve estar em:
// ConnectionStrings:DefaultConnection
// ============================================================

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "A ConnectionStrings:DefaultConnection não foi configurada."
    );
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        connectionString,
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null
            );
        }
    );
});

// ============================================================
// 2. SERVIÇOS DA APLICAÇÃO
// ============================================================

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<ICertificatePdfService, CertificatePdfService>();

// ============================================================
// 3. CONFIGURAÇÃO DE AUTENTICAÇÃO JWT
// ============================================================

var jwtSecretKey = builder.Configuration["Jwt:SecretKey"];

if (string.IsNullOrWhiteSpace(jwtSecretKey))
{
    throw new InvalidOperationException(
        "Jwt:SecretKey não foi configurada."
    );
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SgeIfceApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SgeIfceClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;

    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSecretKey)
        ),

        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,

        ValidateAudience = true,
        ValidAudience = jwtAudience,

        ValidateLifetime = true,

        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "AlunoPolicy",
        policy => policy.RequireRole("Aluno")
    );

    options.AddPolicy(
        "ProfessorPolicy",
        policy => policy.RequireRole("Professor")
    );
});

// ============================================================
// 4. CONFIGURAÇÃO DE CORS
// ============================================================
// As origens devem ser configuradas em:
// Cors:AllowedOrigins
//
// Exemplo local:
// http://localhost:3000
// http://localhost:5173
//
// No deploy, adicionar a URL do frontend da Vercel.
// ============================================================

var allowedOrigins =
    builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()
    ?? Array.Empty<string>();

if (allowedOrigins.Length == 0)
{
    throw new InvalidOperationException(
        "Nenhuma origem foi configurada em Cors:AllowedOrigins."
    );
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("SgeCorsPolicy", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ============================================================
// 5. CONTROLLERS
// ============================================================

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = false;
    });

// ============================================================
// 6. SWAGGER / OPENAPI
// ============================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SGE-IFCE API",
        Version = "v1",
        Description =
            "API REST oficial do Sistema de Gestão de Eventos do IFCE Campus Cedro."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description =
            "Insira o token JWT no formato: Bearer {seu_token}",

        Name = "Authorization",

        In = ParameterLocation.Header,

        Type = SecuritySchemeType.Http,

        Scheme = "bearer",

        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },

                Array.Empty<string>()
            }
        }
    );
});

// ============================================================
// 7. CONSTRUÇÃO DA APLICAÇÃO
// ============================================================

var app = builder.Build();

// ============================================================
// 8. MIGRAÇÃO E INICIALIZAÇÃO DO BANCO
// ============================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        if (context.Database.IsRelational() &&
            !string.Equals(
                context.Database.ProviderName,
                "Microsoft.EntityFrameworkCore.Sqlite",
                StringComparison.OrdinalIgnoreCase))
        {
            await context.Database.MigrateAsync();
        }

        await DbInitializer.InitializeAsync(context);
    }
    catch (Exception ex)
    {
        var logger =
            services.GetRequiredService<ILogger<Program>>();

        logger.LogError(
            ex,
            "Erro ao inicializar o banco de dados."
        );

        throw;
    }
}

// ============================================================
// 9. PIPELINE HTTP
// ============================================================

app.UseMiddleware<GlobalExceptionMiddleware>();

// Swagger
app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "SGE-IFCE API v1"
    );

    c.RoutePrefix = "swagger";
});

// CORS
app.UseCors("SgeCorsPolicy");

// Autenticação
app.UseAuthentication();

// Autorização
app.UseAuthorization();

// Controllers
app.MapControllers();

// ============================================================
// 10. EXECUÇÃO
// ============================================================

app.Run();

// Necessário para testes de integração com WebApplicationFactory
public partial class Program { }