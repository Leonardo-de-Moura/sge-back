using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SgeIfce.Api.Migrations
{
    /// <inheritdoc />
    public partial class ConfirmExistingUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE users
                SET "EmailConfirmed" = TRUE,
                    "EmailConfirmedAt" = COALESCE("EmailConfirmedAt", CURRENT_TIMESTAMP)
                WHERE "EmailConfirmed" = FALSE;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // O estado anterior de confirmação não pode ser reconstruído com segurança.
        }
    }
}
