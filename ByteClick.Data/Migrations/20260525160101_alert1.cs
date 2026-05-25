using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ByteClick.Data.Migrations
{
    /// <inheritdoc />
    public partial class alert1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ProcessDelayMs",
                table: "Alerts",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessDelayMs",
                table: "Alerts");
        }
    }
}
