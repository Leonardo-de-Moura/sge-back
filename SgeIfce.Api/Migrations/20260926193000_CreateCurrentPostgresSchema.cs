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
            DO $migration$
            DECLARE
                expected_tables text[] := ARRAY[
                    'users', 'events', 'activities', 'registrations', 'attendances', 'certificates'
                ];
                existing_table_count integer;
                actual_column_count integer;
            BEGIN
                SELECT count(*)
                INTO existing_table_count
                FROM pg_catalog.pg_class AS relation
                JOIN pg_catalog.pg_namespace AS namespace
                    ON namespace.oid = relation.relnamespace
                WHERE namespace.nspname = current_schema()
                    AND relation.relkind = 'r'
                    AND relation.relname = ANY(expected_tables);

                IF existing_table_count = 0 THEN
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
                ELSIF existing_table_count = cardinality(expected_tables) THEN
                    SELECT count(*)
                    INTO actual_column_count
                    FROM information_schema.columns
                    WHERE table_schema = current_schema()
                        AND table_name = ANY(expected_tables);

                    IF actual_column_count <> 73 OR EXISTS (
                        SELECT 1
                        FROM (VALUES
                            ('users', 'Id', 'character varying', 64, false),
                            ('users', 'Name', 'character varying', 150, false),
                            ('users', 'Email', 'character varying', 150, false),
                            ('users', 'PasswordHash', 'character varying', 255, false),
                            ('users', 'Role', 'character varying', 20, false),
                            ('users', 'Matricula', 'character varying', 50, true),
                            ('users', 'Siape', 'character varying', 50, true),
                            ('users', 'Department', 'character varying', 100, true),
                            ('users', 'Course', 'character varying', 100, true),
                            ('users', 'Phone', 'character varying', 30, true),
                            ('users', 'AvatarUrl', 'character varying', 500, true),
                            ('users', 'CreatedAt', 'timestamp with time zone', NULL, false),
                            ('users', 'UpdatedAt', 'timestamp with time zone', NULL, true),
                            ('events', 'Id', 'character varying', 64, false),
                            ('events', 'Title', 'character varying', 255, false),
                            ('events', 'Description', 'text', NULL, false),
                            ('events', 'Category', 'character varying', 60, false),
                            ('events', 'Modality', 'character varying', 30, false),
                            ('events', 'StartDate', 'timestamp with time zone', NULL, false),
                            ('events', 'EndDate', 'timestamp with time zone', NULL, true),
                            ('events', 'Workload', 'character varying', 50, false),
                            ('events', 'Location', 'character varying', 255, false),
                            ('events', 'TotalSlots', 'integer', NULL, false),
                            ('events', 'EnrolledSlots', 'integer', NULL, false),
                            ('events', 'Status', 'character varying', 30, false),
                            ('events', 'DayMonth', 'character varying', 30, false),
                            ('events', 'ImageUrl', 'character varying', 500, true),
                            ('events', 'OrganizerId', 'character varying', 64, true),
                            ('events', 'CreatedAt', 'timestamp with time zone', NULL, false),
                            ('events', 'UpdatedAt', 'timestamp with time zone', NULL, true),
                            ('activities', 'Id', 'character varying', 64, false),
                            ('activities', 'EventId', 'character varying', 64, false),
                            ('activities', 'Title', 'character varying', 200, false),
                            ('activities', 'Time', 'character varying', 60, false),
                            ('activities', 'Speaker', 'character varying', 150, true),
                            ('activities', 'Location', 'character varying', 200, true),
                            ('activities', 'Order', 'integer', NULL, false),
                            ('activities', 'CreatedAt', 'timestamp with time zone', NULL, false),
                            ('activities', 'UpdatedAt', 'timestamp with time zone', NULL, true),
                            ('registrations', 'Id', 'character varying', 64, false),
                            ('registrations', 'UserId', 'character varying', 64, false),
                            ('registrations', 'EventId', 'character varying', 64, false),
                            ('registrations', 'EventTitle', 'character varying', 255, false),
                            ('registrations', 'RegistrationDate', 'timestamp with time zone', NULL, false),
                            ('registrations', 'Status', 'character varying', 30, false),
                            ('registrations', 'TicketCode', 'character varying', 60, false),
                            ('registrations', 'CreatedAt', 'timestamp with time zone', NULL, false),
                            ('registrations', 'UpdatedAt', 'timestamp with time zone', NULL, true),
                            ('attendances', 'Id', 'character varying', 64, false),
                            ('attendances', 'EventId', 'character varying', 64, false),
                            ('attendances', 'UserId', 'character varying', 64, true),
                            ('attendances', 'ParticipantName', 'character varying', 150, false),
                            ('attendances', 'ParticipantEmail', 'character varying', 150, false),
                            ('attendances', 'Matricula', 'character varying', 50, false),
                            ('attendances', 'Status', 'character varying', 30, false),
                            ('attendances', 'AvatarUrl', 'character varying', 500, true),
                            ('attendances', 'CertificateIssued', 'boolean', NULL, false),
                            ('attendances', 'CheckedInAt', 'timestamp with time zone', NULL, true),
                            ('attendances', 'CreatedAt', 'timestamp with time zone', NULL, false),
                            ('attendances', 'UpdatedAt', 'timestamp with time zone', NULL, true),
                            ('certificates', 'Id', 'character varying', 64, false),
                            ('certificates', 'EventId', 'character varying', 64, false),
                            ('certificates', 'UserId', 'character varying', 64, true),
                            ('certificates', 'EventTitle', 'character varying', 255, false),
                            ('certificates', 'ParticipantName', 'character varying', 150, false),
                            ('certificates', 'ParticipantEmail', 'character varying', 150, false),
                            ('certificates', 'Matricula', 'character varying', 50, true),
                            ('certificates', 'IssueDate', 'timestamp with time zone', NULL, false),
                            ('certificates', 'Workload', 'character varying', 50, false),
                            ('certificates', 'ValidationCode', 'character varying', 80, false),
                            ('certificates', 'PdfUrl', 'character varying', 500, true),
                            ('certificates', 'CreatedAt', 'timestamp with time zone', NULL, false),
                            ('certificates', 'UpdatedAt', 'timestamp with time zone', NULL, true)
                        ) AS expected(table_name, column_name, data_type, max_length, nullable)
                        LEFT JOIN information_schema.columns AS actual
                            ON actual.table_schema = current_schema()
                            AND actual.table_name = expected.table_name
                            AND actual.column_name = expected.column_name
                        WHERE actual.column_name IS NULL
                            OR actual.data_type IS DISTINCT FROM expected.data_type
                            OR actual.character_maximum_length IS DISTINCT FROM expected.max_length
                            OR (actual.is_nullable = 'YES') IS DISTINCT FROM expected.nullable
                    ) THEN
                        RAISE EXCEPTION
                            'Existing SGE tables do not match migration 20260926193000; no schema changes were made.';
                    END IF;

                    IF EXISTS (
                        SELECT required.index_name
                        FROM (VALUES
                            ('PK_users'), ('IX_users_Email'), ('IX_users_Matricula'), ('IX_users_Siape'),
                            ('PK_events'), ('IX_events_OrganizerId'),
                            ('PK_activities'), ('IX_activities_EventId'),
                            ('PK_registrations'), ('IX_registrations_EventId'),
                            ('IX_registrations_TicketCode'), ('IX_registrations_UserId'),
                            ('PK_attendances'), ('IX_attendances_EventId'), ('IX_attendances_UserId'),
                            ('PK_certificates'), ('IX_certificates_EventId'),
                            ('IX_certificates_UserId'), ('IX_certificates_ValidationCode')
                        ) AS required(index_name)
                        LEFT JOIN pg_catalog.pg_indexes AS actual
                            ON actual.schemaname = current_schema()
                            AND actual.indexname = required.index_name
                        WHERE actual.indexname IS NULL
                    ) OR EXISTS (
                        SELECT required.constraint_name
                        FROM (VALUES
                            ('FK_events_users_OrganizerId'),
                            ('FK_activities_events_EventId'),
                            ('FK_registrations_users_UserId'),
                            ('FK_registrations_events_EventId'),
                            ('FK_attendances_events_EventId'),
                            ('FK_attendances_users_UserId'),
                            ('FK_certificates_events_EventId'),
                            ('FK_certificates_users_UserId')
                        ) AS required(constraint_name)
                        LEFT JOIN pg_catalog.pg_constraint AS actual
                            ON actual.connamespace = (
                                SELECT oid
                                FROM pg_catalog.pg_namespace
                                WHERE nspname = current_schema()
                            )
                            AND actual.conname = required.constraint_name
                            AND actual.contype = 'f'
                        WHERE actual.oid IS NULL
                    ) THEN
                        RAISE EXCEPTION
                            'Existing SGE indexes or foreign keys do not match migration 20260926193000; no schema changes were made.';
                    END IF;
                ELSE
                    RAISE EXCEPTION
                        'Only part of the SGE schema exists; migration 20260926193000 made no schema changes.';
                END IF;
            END
            $migration$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}