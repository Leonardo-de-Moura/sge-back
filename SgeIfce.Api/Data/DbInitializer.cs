using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using SgeIfce.Api.Models;

namespace SgeIfce.Api.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        if (context.Database.IsRelational() && !string.Equals(context.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                await context.Database.MigrateAsync();
            }
            catch
            {
                await context.Database.EnsureCreatedAsync();
            }
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }

        if (await context.Users.AnyAsync())
        {
            return; // Banco já semeado
        }

        var defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("123456");

        // 1. Usuários
        var alunoLuzia = new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Luzia Fernandes",
            Email = "luzia@aluno.ifce.edu.br",
            PasswordHash = defaultPasswordHash,
            Role = "Aluno",
            Matricula = "2023108922",
            CreatedAt = DateTime.UtcNow
        };

        var profRicardo = new User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name = "Prof. Dr. Ricardo Silva",
            Email = "ricardo.silva@ifce.edu.br",
            PasswordHash = defaultPasswordHash,
            Role = "Professor",
            Siape = "1849201",
            CreatedAt = DateTime.UtcNow
        };

        var alunoAline = new User
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333331"),
            Name = "Aline de Sousa Silva",
            Email = "aline.sousa@aluno.ifce.edu.br",
            PasswordHash = defaultPasswordHash,
            Role = "Aluno",
            Matricula = "2023104501",
            CreatedAt = DateTime.UtcNow
        };

        var alunoBruno = new User
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333332"),
            Name = "Bruno Lima Oliveira",
            Email = "bruno.lima@aluno.ifce.edu.br",
            PasswordHash = defaultPasswordHash,
            Role = "Aluno",
            Matricula = "2022108912",
            CreatedAt = DateTime.UtcNow
        };

        var alunoCamila = new User
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Name = "Camila Rocha Cavalcante",
            Email = "camila.rocha@aluno.ifce.edu.br",
            PasswordHash = defaultPasswordHash,
            Role = "Aluno",
            Matricula = "2023201103",
            CreatedAt = DateTime.UtcNow
        };

        var alunoDiego = new User
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333334"),
            Name = "Diego Ferreira Gomes",
            Email = "diego.gomes@aluno.ifce.edu.br",
            PasswordHash = defaultPasswordHash,
            Role = "Aluno",
            Matricula = "2021104490",
            CreatedAt = DateTime.UtcNow
        };

        var alunoEduarda = new User
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333335"),
            Name = "Eduarda Alves Parente",
            Email = "eduarda.parente@aluno.ifce.edu.br",
            PasswordHash = defaultPasswordHash,
            Role = "Aluno",
            Matricula = "2024102219",
            CreatedAt = DateTime.UtcNow
        };

        var alunoFelipe = new User
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333336"),
            Name = "Felipe Pinheiro Santos",
            Email = "felipe.santos@aluno.ifce.edu.br",
            PasswordHash = defaultPasswordHash,
            Role = "Aluno",
            Matricula = "2022209931",
            CreatedAt = DateTime.UtcNow
        };

        await context.Users.AddRangeAsync(
            alunoLuzia, profRicardo, alunoAline, alunoBruno, alunoCamila, alunoDiego, alunoEduarda, alunoFelipe
        );

        // 2. Eventos
        var event1 = new Event
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444441"),
            Title = "Inteligência Artificial e o Futuro da Educação",
            Description = "Aprenda sobre o impacto dos grandes modelos de linguagem e ferramentas de IA aplicadas ao aprendizado e à docência acadêmica.",
            Category = "Palestra",
            Modality = "Presencial",
            StartDate = new DateTime(2026, 5, 21, 8, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 5, 21, 12, 0, 0, DateTimeKind.Utc),
            Workload = "4 horas",
            Location = "Auditório Principal - IFCE Campus Cedro",
            TotalSlots = 60,
            Status = "Aberto",
            OrganizerId = profRicardo.Id,
            CreatedAt = DateTime.UtcNow
        };

        var event2 = new Event
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444442"),
            Title = "Desenvolvimento Web Moderno com React & Tailwind",
            Description = "Minicurso prático com foco na criação de interfaces modernas, acessíveis e responsivas para o ecossistema web.",
            Category = "Minicurso",
            Modality = "Híbrido",
            StartDate = new DateTime(2026, 5, 28, 14, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 5, 28, 18, 0, 0, DateTimeKind.Utc),
            Workload = "8 horas",
            Location = "Laboratório de Informática 03 - IFCE",
            TotalSlots = 30,
            Status = "Esgotado",
            OrganizerId = profRicardo.Id,
            CreatedAt = DateTime.UtcNow
        };

        var event3 = new Event
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444443"),
            Title = "Semana da Inovação e Sustentabilidade no Semiárido",
            Description = "Grandes debates, oficinas práticas e apresentações de artigos científicos com pesquisadores de renome regional e nacional.",
            Category = "Congresso",
            Modality = "Presencial",
            StartDate = new DateTime(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 6, 12, 18, 0, 0, DateTimeKind.Utc),
            Workload = "20 horas",
            Location = "Campus Central do IFCE Cedro",
            TotalSlots = 150,
            Status = "Aberto",
            OrganizerId = profRicardo.Id,
            CreatedAt = DateTime.UtcNow
        };

        var event4 = new Event
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Title = "Oficina Prática de Robótica com Arduino",
            Description = "Aprenda lógica de programação e eletrônica básica criando pequenos protótipos de automação residencial e sensores.",
            Category = "Oficina",
            Modality = "Presencial",
            StartDate = new DateTime(2026, 6, 25, 9, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 6, 25, 13, 0, 0, DateTimeKind.Utc),
            Workload = "4 horas",
            Location = "Espaço Maker - IFCE Cedro",
            TotalSlots = 25,
            Status = "Aberto",
            OrganizerId = profRicardo.Id,
            CreatedAt = DateTime.UtcNow
        };

        await context.Events.AddRangeAsync(event1, event2, event3, event4);

        // 3. Atividades
        var activities = new List<Activity>
        {
            new Activity
            {
                Id = Guid.NewGuid(),
                EventId = event1.Id,
                Title = "Credenciamento e Recepção",
                Time = "08:00 - 08:30",
                Speaker = "Comissão Organizadora",
                Location = "Hall do Auditório"
            },
            new Activity
            {
                Id = Guid.NewGuid(),
                EventId = event1.Id,
                Title = "Palestra Magna: IA Generativa e Modelos LLM",
                Time = "08:30 - 10:30",
                Speaker = "Prof. Dr. Ricardo Silva",
                Location = "Auditório Principal"
            },
            new Activity
            {
                Id = Guid.NewGuid(),
                EventId = event1.Id,
                Title = "Mesa Redonda: Ética e Inovação na Educação",
                Time = "10:45 - 12:00",
                Speaker = "Convidados Especiais",
                Location = "Auditório Principal"
            },
            new Activity
            {
                Id = Guid.NewGuid(),
                EventId = event3.Id,
                Title = "Cerimônia de Abertura e Keynote",
                Time = "08:00 - 10:00",
                Speaker = "Reitoria e Diretoria do Campus",
                Location = "Ginásio Poliesportivo"
            },
            new Activity
            {
                Id = Guid.NewGuid(),
                EventId = event3.Id,
                Title = "Apresentação de Trabalhos de Iniciação Científica",
                Time = "10:30 - 12:30",
                Speaker = "Discentes e Orientadores",
                Location = "Salas Temáticas Bloco B"
            }
        };

        await context.Activities.AddRangeAsync(activities);

        // 4. Inscrições
        var reg1 = new Registration
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555551"),
            UserId = alunoLuzia.Id,
            EventId = event1.Id,
            TicketCode = "SGE-IA-94812",
            Status = "confirmado",
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        var reg2 = new Registration
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555552"),
            UserId = alunoLuzia.Id,
            EventId = event3.Id,
            TicketCode = "SGE-INOV-88231",
            Status = "confirmado",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var regAline = new Registration
        {
            Id = Guid.NewGuid(),
            UserId = alunoAline.Id,
            EventId = event1.Id,
            TicketCode = "SGE-IA-10001",
            Status = "confirmado",
            CreatedAt = DateTime.UtcNow.AddDays(-4)
        };

        var regBruno = new Registration
        {
            Id = Guid.NewGuid(),
            UserId = alunoBruno.Id,
            EventId = event1.Id,
            TicketCode = "SGE-IA-10002",
            Status = "confirmado",
            CreatedAt = DateTime.UtcNow.AddDays(-4)
        };

        var regCamila = new Registration
        {
            Id = Guid.NewGuid(),
            UserId = alunoCamila.Id,
            EventId = event1.Id,
            TicketCode = "SGE-IA-10003",
            Status = "confirmado",
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };

        var regDiego = new Registration
        {
            Id = Guid.NewGuid(),
            UserId = alunoDiego.Id,
            EventId = event1.Id,
            TicketCode = "SGE-IA-10004",
            Status = "confirmado",
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };

        await context.Registrations.AddRangeAsync(reg1, reg2, regAline, regBruno, regCamila, regDiego);

        // 5. Presenças
        var attLuzia = new Attendance
        {
            Id = Guid.NewGuid(),
            EventId = event1.Id,
            UserId = alunoLuzia.Id,
            RegistrationId = reg1.Id,
            Status = "presente",
            CertificateIssued = true,
            CheckedInAt = DateTime.UtcNow.AddDays(-3)
        };

        var attAline = new Attendance
        {
            Id = Guid.Parse("66666666-6666-6666-6666-666666666661"),
            EventId = event1.Id,
            UserId = alunoAline.Id,
            RegistrationId = regAline.Id,
            Status = "presente",
            CertificateIssued = true,
            CheckedInAt = DateTime.UtcNow.AddDays(-3)
        };

        var attBruno = new Attendance
        {
            Id = Guid.Parse("66666666-6666-6666-6666-666666666662"),
            EventId = event1.Id,
            UserId = alunoBruno.Id,
            RegistrationId = regBruno.Id,
            Status = "presente",
            CertificateIssued = false,
            CheckedInAt = DateTime.UtcNow.AddDays(-3)
        };

        var attCamila = new Attendance
        {
            Id = Guid.Parse("66666666-6666-6666-6666-666666666663"),
            EventId = event1.Id,
            UserId = alunoCamila.Id,
            RegistrationId = regCamila.Id,
            Status = "presente",
            CertificateIssued = true,
            CheckedInAt = DateTime.UtcNow.AddDays(-3)
        };

        var attDiego = new Attendance
        {
            Id = Guid.Parse("66666666-6666-6666-6666-666666666664"),
            EventId = event1.Id,
            UserId = alunoDiego.Id,
            RegistrationId = regDiego.Id,
            Status = "ausente",
            CertificateIssued = false,
            CheckedInAt = null
        };

        await context.Attendances.AddRangeAsync(attLuzia, attAline, attBruno, attCamila, attDiego);

        // 6. Certificados
        var cert1 = new Certificate
        {
            Id = Guid.Parse("77777777-7777-7777-7777-777777777771"),
            EventId = event1.Id,
            UserId = alunoLuzia.Id,
            AttendanceId = attLuzia.Id,
            Code = "IFCE-CED-2026-CERT-8841",
            EventTitle = "Inteligência Artificial e o Futuro da Educação",
            ParticipantName = "Luzia Fernandes",
            Workload = "4 horas",
            IssueDate = DateTime.UtcNow.AddDays(-2)
        };

        var cert2 = new Certificate
        {
            Id = Guid.Parse("77777777-7777-7777-7777-777777777772"),
            EventId = event2.Id,
            UserId = alunoLuzia.Id,
            Code = "IFCE-CED-2026-CERT-7712",
            EventTitle = "Workshop de Acessibilidade na Web",
            ParticipantName = "Luzia Fernandes",
            Workload = "6 horas",
            IssueDate = DateTime.UtcNow.AddDays(-10)
        };

        await context.Certificates.AddRangeAsync(cert1, cert2);

        await context.SaveChangesAsync();
    }
}
