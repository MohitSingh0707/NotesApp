using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotesApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkUserAndDeviceTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DeviceTokens_UserId",
                table: "DeviceTokens",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceTokens_Users_UserId",
                table: "DeviceTokens",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeviceTokens_Users_UserId",
                table: "DeviceTokens");

            migrationBuilder.DropIndex(
                name: "IX_DeviceTokens_UserId",
                table: "DeviceTokens");
        }
    }
}
