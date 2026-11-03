# SGE-IFCE — Backend API (.NET 8)

API RESTful oficial do **Sistema de Gestão de Eventos (SGE)** do **Instituto Federal do Ceará (IFCE) - Campus Cedro**, desenvolvida em **ASP.NET Core 8**, **Entity Framework Core**, **JWT Authentication** e geração dinâmica de certificados em PDF.

---

## 🚀 Tecnologias

- **C# / .NET 8 SDK** (ASP.NET Core Web API)
- **Entity Framework Core 8** (Code-First; a inicialização atual da API usa PostgreSQL)
- **BCrypt.Net-Next** (Hashing seguro de senhas)
- **System.IdentityModel.Tokens.Jwt** (Autenticação JWT Bearer)
- **PDF vetorial** (certificado A4 horizontal no padrão visual IFCE, com moldura verde/vermelha, marca institucional, assinatura do organizador e código verificador)
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
- PostgreSQL acessível localmente ou por uma connection string válida.

### 2. Restaurar dependências e compilar
```bash
dotnet restore SgeIfce.Api/SgeIfce.Api.csproj
dotnet build SgeIfce.Api/SgeIfce.Api.csproj
```

### 3. Configurar PostgreSQL, CORS e segredo JWT

A inicialização atual exige uma connection string PostgreSQL em
`ConnectionStrings__DefaultConnection`, pelo menos uma origem em
`Cors__AllowedOrigins__0` e um segredo em `Jwt__SecretKey`. O segredo precisa
conter pelo menos 32 bytes UTF-8. Configure esses valores no ambiente antes de
iniciar a API; por exemplo, em uma sessão local com PostgreSQL na máquina:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=sge_ifce_db;Username=postgres;Password=postgres"
export Cors__AllowedOrigins__0="http://localhost:5173"
export Jwt__SecretKey="$(openssl rand -base64 48)"
```

Use credenciais correspondentes à sua instalação PostgreSQL. O banco deve estar
disponível e acessível pela connection string; a API aplica as migrations durante
a inicialização. A execução direta com `dotnet run` não cria um banco SQLite.

Não reutilize esse exemplo como mecanismo de provisionamento em produção, não
inclua o valor em arquivos versionados e armazene segredos de implantação no
gerenciador de secrets da plataforma.

### 4. Executar a API
```bash
dotnet run --project SgeIfce.Api/SgeIfce.Api.csproj --urls "http://localhost:5000"
```

A API estará disponível em:
- **API Base:** `http://localhost:5000/api`
- **Swagger UI:** `http://localhost:5000/swagger`
- **Health Check:** `http://localhost:5000/health`

Em ambiente `Development`, a API executa o seed de demonstração se o banco
estiver vazio. Fora desse ambiente, o seed e a conversão de senhas legadas são
ignorados.

---

## 🐳 Executando com Docker & Docker Compose

Pré-requisitos: Docker Engine e o plugin Docker Compose instalados e em execução.

Na pasta `dist-repos/sge-ifce-backend`, suba a API e o PostgreSQL:

```bash
export JWT_SECRET_KEY="$(openssl rand -base64 48)"
docker compose up --build
```

O Compose exige `JWT_SECRET_KEY` e a repassa à API como `Jwt__SecretKey`.
O segredo não deve ser salvo no repositório. O Compose define
`ASPNETCORE_ENVIRONMENT=Development`; por isso, o banco de desenvolvimento pode
receber os dados de demonstração. O seed e a conversão de senhas legadas são
ignorados fora do ambiente Development.

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

Estas contas são criadas somente pelo seed no ambiente Development. Elas não são
provisionadas em produção.

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

## 🚀 Configuração no Render

Configure `Jwt__SecretKey` como uma variável secreta do serviço da API no painel
do Render, com pelo menos 32 bytes UTF-8. Configure também a connection string e
as origens CORS conforme o ambiente. Não inclua o valor JWT em commits, logs,
documentação pública ou variáveis expostas ao frontend.

Alterar o segredo invalida todos os JWT assinados com o valor anterior; usuários
com sessões ativas precisarão autenticar novamente. A rotação de produção não foi
executada. Antes dela, planeje uma janela de reautenticação, atualize o secret no
Render e reinicie/reimplante a API somente após a aprovação operacional.

## 📱 Check-in por QR Code

O professor autenticado gera o QR Code do evento por `POST /api/events/{eventId}/qrcode`.
O código abre a rota `/aluno/check-in` do frontend, onde o aluno autenticado confirma a
presença. A API recebe o token em `POST /api/attendance/check-in`; o token deve estar
ativo e o aluno precisa ter inscrição no evento. As colunas do QR Code são aplicadas
automaticamente pela migration na inicialização da API.
O endereço de check-in usa a origem atual do frontend, sem exigir uma configuração
adicional específica para QR Code.
