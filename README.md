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

Pré-requisitos: Docker Engine e o plugin Docker Compose instalados e em execução.

Na pasta `dist-repos/sge-ifce-backend`, suba a API e o PostgreSQL:

```bash
docker compose up --build
```

O Compose aguarda o PostgreSQL ficar saudável. Na primeira inicialização, a API
aplica as migrations do Entity Framework e semeia o banco. A API estará disponível
em `http://localhost:5000/swagger`; o PostgreSQL fica acessível no host pela porta
`5433` (dentro da rede Docker, a API usa `db:5432`).

O pgAdmin também estará disponível em `http://localhost:5050`. Entre com `admin@sge-ifce.com`
e `sge-local-admin` e registre uma conexão PostgreSQL com estes dados:

| Campo | Valor |
|---|---|
| Host | `db` |
| Porta | `5432` |
| Banco de manutenção | `sge_ifce_db` |
| Usuário | `postgres` |
| Senha | `postgres` |

O host `db` é o nome do serviço dentro da rede Docker. Para credenciais diferentes, configure
`POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `PGADMIN_DEFAULT_EMAIL` e
`PGADMIN_DEFAULT_PASSWORD` no ambiente antes de subir os serviços. A porta do pgAdmin está
vinculada a `127.0.0.1`, portanto não fica exposta à rede local.

Para acompanhar os serviços, use `docker compose ps` e `docker compose logs -f`.
Para pará-los, pressione `Ctrl+C` e execute `docker compose down`. Os dados continuam
no volume `pgdata`; para apagá-los também, execute `docker compose down -v`.

É possível configurar `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` e
`POSTGRES_PORT` no ambiente antes de subir os serviços. Os valores padrão são para
desenvolvimento local; defina uma senha própria fora desse contexto.

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
