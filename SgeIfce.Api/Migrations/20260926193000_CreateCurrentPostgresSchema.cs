using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SgeIfce.Api.Data;

#nullable disable

namespace SgeIfce.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260926193000_CreateCurrentPostgresSchema")]
public partial class CreateCurrentPostgresSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE users (
                "Id" character varying(64) NOT NULL CONSTRAINT "PK_users" PRIMARY KEY,
                "Name" character varying(150) NOT NULL,
                "Email" character varying(150) NOT NULL,
                "PasswordHash" character varying(255) NOT NULL,
                "Role" character varying(20) NOT NULL,
                "Matricula" character varying(50),
                "Siape" character varying(50),
                "Department" character varying(100),
                "Course" character varying(100),
                "Phone" character varying(30),
                "AvatarUrl" character varying(500),
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone
            );

            CREATE TABLE events (
                "Id" character varying(64) NOT NULL CONSTRAINT "PK_events" PRIMARY KEY,
                "Title" character varying(255) NOT NULL,
                "Description" text NOT NULL,
                "Category" character varying(60) NOT NULL,
                "Modality" character varying(30) NOT NULL,
                "StartDate" timestamp with time zone NOT NULL,
                "EndDate" timestamp with time zone,
                "Workload" character varying(50) NOT NULL,
                "Location" character varying(255) NOT NULL,
                "TotalSlots" integer NOT NULL,
                "EnrolledSlots" integer NOT NULL,
                "Status" character varying(30) NOT NULL,
                "DayMonth" character varying(30) NOT NULL,
                "ImageUrl" character varying(500),
                "OrganizerId" character varying(64),
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone,
                CONSTRAINT "FK_events_users_OrganizerId"
                    FOREIGN KEY ("OrganizerId") REFERENCES users ("Id") ON DELETE SET NULL
            );

            CREATE TABLE activities (
                "Id" character varying(64) NOT NULL CONSTRAINT "PK_activities" PRIMARY KEY,
                "EventId" character varying(64) NOT NULL,
                "Title" character varying(200) NOT NULL,
                "Time" character varying(60) NOT NULL,
                "Speaker" character varying(150),
                "Location" character varying(200),
                "Order" integer NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone,
                CONSTRAINT "FK_activities_events_EventId"
                    FOREIGN KEY ("EventId") REFERENCES events ("Id") ON DELETE CASCADE
            );

            CREATE TABLE registrations (
                "Id" character varying(64) NOT NULL CONSTRAINT "PK_registrations" PRIMARY KEY,
                "UserId" character varying(64) NOT NULL,
                "EventId" character varying(64) NOT NULL,
                "EventTitle" character varying(255) NOT NULL,
                "RegistrationDate" timestamp with time zone NOT NULL,
                "Status" character varying(30) NOT NULL,
                "TicketCode" character varying(60) NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone,
                CONSTRAINT "FK_registrations_users_UserId"
                    FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_registrations_events_EventId"
                    FOREIGN KEY ("EventId") REFERENCES events ("Id") ON DELETE CASCADE
            );

            CREATE TABLE attendances (
                "Id" character varying(64) NOT NULL CONSTRAINT "PK_attendances" PRIMARY KEY,
                "EventId" character varying(64) NOT NULL,
                "UserId" character varying(64),
                "ParticipantName" character varying(150) NOT NULL,
                "ParticipantEmail" character varying(150) NOT NULL,
                "Matricula" character varying(50) NOT NULL,
                "Status" character varying(30) NOT NULL,
                "AvatarUrl" character varying(500),
                "CertificateIssued" boolean NOT NULL,
                "CheckedInAt" timestamp with time zone,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone,
                CONSTRAINT "FK_attendances_events_EventId"
                    FOREIGN KEY ("EventId") REFERENCES events ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_attendances_users_UserId"
                    FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE SET NULL
            );

            CREATE TABLE certificates (
                "Id" character varying(64) NOT NULL CONSTRAINT "PK_certificates" PRIMARY KEY,
                "EventId" character varying(64) NOT NULL,
                "UserId" character varying(64),
                "EventTitle" character varying(255) NOT NULL,
                "ParticipantName" character varying(150) NOT NULL,
                "ParticipantEmail" character varying(150) NOT NULL,
                "Matricula" character varying(50),
                "IssueDate" timestamp with time zone NOT NULL,
                "Workload" character varying(50) NOT NULL,
                "ValidationCode" character varying(80) NOT NULL,
                "PdfUrl" character varying(500),
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone,
                CONSTRAINT "FK_certificates_events_EventId"
                    FOREIGN KEY ("EventId") REFERENCES events ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_certificates_users_UserId"
                    FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE SET NULL
            );

            CREATE UNIQUE INDEX "IX_users_Email" ON users ("Email");
            CREATE INDEX "IX_users_Matricula" ON users ("Matricula");
            CREATE INDEX "IX_users_Siape" ON users ("Siape");
            CREATE INDEX "IX_events_OrganizerId" ON events ("OrganizerId");
            CREATE INDEX "IX_activities_EventId" ON activities ("EventId");
            CREATE INDEX "IX_registrations_EventId" ON registrations ("EventId");
            CREATE UNIQUE INDEX "IX_registrations_TicketCode" ON registrations ("TicketCode");
            CREATE INDEX "IX_registrations_UserId" ON registrations ("UserId");
            CREATE INDEX "IX_attendances_EventId" ON attendances ("EventId");
            CREATE INDEX "IX_attendances_UserId" ON attendances ("UserId");
            CREATE INDEX "IX_certificates_EventId" ON certificates ("EventId");
            CREATE INDEX "IX_certificates_UserId" ON certificates ("UserId");
            CREATE UNIQUE INDEX "IX_certificates_ValidationCode" ON certificates ("ValidationCode");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE certificates;
            DROP TABLE attendances;
            DROP TABLE registrations;
            DROP TABLE activities;
            DROP TABLE events;
            DROP TABLE users;
            """);
    }
}