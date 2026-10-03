using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SgeIfce.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEventQrCodePersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "QrCodeExpiresAt",
                table: "events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "QrCodeGeneratedAt",
                table: "events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QrCodeToken",
                table: "events",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QrCodeExpiresAt",
                table: "events");

            migrationBuilder.DropColumn(
                name: "QrCodeGeneratedAt",
                table: "events");

            migrationBuilder.DropColumn(
                name: "QrCodeToken",
                table: "events");
        }
    }
}
