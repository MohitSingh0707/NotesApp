using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotesApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserLevelNoteAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessibleFrom",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "AccessibleTill",
                table: "Notes");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Notes",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Notes");

            migrationBuilder.AddColumn<DateTime>(
                name: "AccessibleFrom",
                table: "Notes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AccessibleTill",
                table: "Notes",
                type: "datetime2",
                nullable: true);
        }
    }
}
