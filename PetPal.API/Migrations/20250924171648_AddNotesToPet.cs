using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetPal.API.Migrations
{
    /// <inheritdoc />
    public partial class AddNotesToPet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Pets",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Pets");
        }
    }
}
