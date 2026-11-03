using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SgeIfce.Api.Migrations
{
    /// <inheritdoc />
    public partial class EnforceRegistrationAndCapacity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $migration$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM registrations
                        WHERE "Status" = 'confirmado'
                        GROUP BY "UserId", "EventId"
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION
                            'Duplicate active registrations exist; reconcile them before applying migration 20261103174057.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM attendances
                        WHERE "UserId" IS NOT NULL
                        GROUP BY "EventId", "UserId"
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION
                            'Duplicate user/event attendance records exist; reconcile them before applying migration 20261103174057.';
                    END IF;
                END
                $migration$;

                UPDATE events AS event
                SET "EnrolledSlots" = (
                    SELECT COUNT(*)::integer
                    FROM registrations AS registration
                    WHERE registration."EventId" = event."Id"
                        AND registration."Status" = 'confirmado'
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_registrations_UserId_EventId_Active",
                table: "registrations",
                columns: new[] { "UserId", "EventId" },
                unique: true,
                filter: "\"Status\" = 'confirmado'");

            migrationBuilder.CreateIndex(
                name: "IX_attendances_EventId_UserId",
                table: "attendances",
                columns: new[] { "EventId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_registrations_UserId_EventId_Active",
                table: "registrations");

            migrationBuilder.DropIndex(
                name: "IX_attendances_EventId_UserId",
                table: "attendances");
        }
    }
}
