using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotesApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultipleFilesAndImagesSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImagePath",
                table: "Notes",
                newName: "ImagePaths");

            migrationBuilder.RenameColumn(
                name: "FilePath",
                table: "Notes",
                newName: "FilePaths");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImagePaths",
                table: "Notes",
                newName: "ImagePath");

            migrationBuilder.RenameColumn(
                name: "FilePaths",
                table: "Notes",
                newName: "FilePath");
        }
    }
}
