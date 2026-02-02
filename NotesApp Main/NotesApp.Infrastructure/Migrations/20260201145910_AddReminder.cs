using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotesApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReminder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "Reminders",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "ReminderAtUtc",
                table: "Reminders",
                newName: "RemindAt");

            migrationBuilder.RenameColumn(
                name: "IsTriggered",
                table: "Reminders",
                newName: "IsCompleted");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "Reminders",
                newName: "CreatedAt");

            migrationBuilder.AlterColumn<Guid>(
                name: "NoteId",
                table: "Reminders",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Reminders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCancelled",
                table: "Reminders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "JobId",
                table: "Reminders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Reminders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_UserId_RemindAt",
                table: "Reminders",
                columns: new[] { "UserId", "RemindAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reminders_UserId_RemindAt",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "IsCancelled",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "JobId",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Reminders");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Reminders",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "RemindAt",
                table: "Reminders",
                newName: "ReminderAtUtc");

            migrationBuilder.RenameColumn(
                name: "IsCompleted",
                table: "Reminders",
                newName: "IsTriggered");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Reminders",
                newName: "CreatedAtUtc");

            migrationBuilder.AlterColumn<Guid>(
                name: "NoteId",
                table: "Reminders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
