using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartFarmSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Resources",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Resources");
        }
    }
}
