using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoStreamingAPI.Migrations
{
    /// <inheritdoc />
    public partial class addphotoPathforactormodel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoPath",
                table: "Actors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoPath",
                table: "Actors");
        }
    }
}
