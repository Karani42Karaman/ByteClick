using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ByteClick.Data.Migrations
{
    /// <inheritdoc />
    public partial class alert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "Alerts",
                newName: "TVTimestamp");

            migrationBuilder.AddColumn<double>(
                name: "DelayMs",
                table: "Alerts",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Exchange",
                table: "Alerts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Interval",
                table: "Alerts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Volume",
                table: "Alerts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DelayMs",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "Exchange",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "Interval",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "Volume",
                table: "Alerts");

            migrationBuilder.RenameColumn(
                name: "TVTimestamp",
                table: "Alerts",
                newName: "Timestamp");
        }
    }
}
