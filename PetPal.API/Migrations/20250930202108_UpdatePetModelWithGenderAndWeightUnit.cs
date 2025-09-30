using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PetPal.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePetModelWithGenderAndWeightUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Pets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WeightUnit",
                table: "Pets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ThemePreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserProfileId = table.Column<int>(type: "integer", nullable: false),
                    Theme = table.Column<string>(type: "text", nullable: true),
                    AccentColor = table.Column<string>(type: "text", nullable: true),
                    FontSize = table.Column<string>(type: "text", nullable: true),
                    UseSystemPreference = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThemePreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThemePreferences_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ThemePreferences_UserProfileId",
                table: "ThemePreferences",
                column: "UserProfileId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ThemePreferences");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "WeightUnit",
                table: "Pets");
        }
    }
}
