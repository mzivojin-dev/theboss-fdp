using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoaieDeParcurs.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGoogleMapsApiKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoogleMapsApiKey",
                table: "VehicleProfiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoogleMapsApiKey",
                table: "VehicleProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
