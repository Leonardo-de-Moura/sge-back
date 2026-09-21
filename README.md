# SGE-IFCE — Backend API (.NET 8)

API RESTful oficial do **Sistema de Gestão de Eventos (SGE)** do **Instituto Federal do Ceará (IFCE) - Campus Cedro**, desenvolvida em **ASP.NET Core 8**, **Entity Framework Core**, **JWT Authentication** e geração dinâmica de certificados em PDF.

---

## 🚀 Tecnologias

- **C# / .NET 8 SDK** (ASP.NET Core Web API)
- **Entity Framework Core 8** (Code-First com suporte a SQLite e PostgreSQL)
- **BCrypt.Net-Next** (Hashing seguro de senhas)
- **System.IdentityModel.Tokens.Jwt** (Autenticação JWT Bearer)
- **QuestPDF** / Canvas PDF (Emissão de certificados autenticados)
- **Swagger / OpenAPI** com autorização Bearer integrada
- **Docker & Docker Compose**

---

## 📂 Estrutura do Repositório

```text
backend/
├── SgeIfce.Api/
│   ├── Controllers/         # Endpoints REST (Auth, Events, Registrations, Attendance, Certificates)
│   ├── Data/                # AppDbContext, Migrations e DbInitializer (seed automático)
│   ├── DTOs/                # Data Transfer Objects (Requests & Responses)
│   ├── Middleware/          # GlobalExceptionMiddleware (padronização de erros RFC 7807)
│   ├── Models/              # Entidades de Domínio (User, EventItem, Registration, etc.)
│   ├── Services/            # TokenService, BcryptHasher, CertificatePdfService
│   ├── appsettings.json     # Configurações de banco, JWT e CORS
│   └── Program.cs           # Configuração de DI, Middleware e Pipeline HTTP
├── Dockerfile               # Build multi-stage para produção
├── docker-compose.yml       # Orquestração com PostgreSQL
└── README.md
```

---

## ⚡ Como Executar Localmente

### 1. Pré-requisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) instalado.

### 2. Restaurar dependências e compilar
```bash
dotnet restore SgeIfce.Api/SgeIfce.Api.csproj
dotnet build SgeIfce.Api/SgeIfce.Api.csproj
```

### 3. Executar a API
```bash
dotnet run --project SgeIfce.Api/SgeIfce.Api.csproj --urls "http://localhost:5000"
```

A API estará disponível em:
- **API Base:** `http://localhost:5000/api`
- **Swagger UI:** `http://localhost:5000/swagger`
- **Health Check:** `http://localhost:5000/health`

*Na primeira execução, o banco de dados SQLite (`sge_ifce.db`) será criado e semeado automaticamente.*

---

## 🐳 Executando com Docker & Docker Compose

Para rodar a API com um banco PostgreSQL real conteinerizado:

```bash
docker compose up --build
```

---

## 🔑 Credenciais de Teste Pré-cadastradas

| Perfil | E-mail | Senha | Identificação |
|---|---|---|---|
| **Aluno (Discente)** | `luzia@aluno.ifce.edu.br` | `123456` | Matrícula: `2023108922` |
| **Professor (Docente)** | `ricardo.silva@ifce.edu.br` | `123456` | SIAPE: `1849201` |

---

## 🌐 Configuração de CORS

O CORS permite origens padrão para os clientes web:
- `http://localhost:3000`
- `http://localhost:5173` (Vite)

Para adicionar novas origens em produção, configure a variável de ambiente:
```bash
Cors__AllowedOrigins__0=https://meu-frontend.vercel.app
```
