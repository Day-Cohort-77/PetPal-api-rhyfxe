using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetPal.API.Migrations
{
    /// <inheritdoc />
    public partial class AddDurationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Duration",
                table: "TrainingProgress",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DurationType",
                table: "TrainingProgress",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                table: "TrainingProgress");

            migrationBuilder.DropColumn(
                name: "DurationType",
                table: "TrainingProgress");
        }
    }
}
