using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoaieDeParcurs.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FillUps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    StationLocationId = table.Column<int>(type: "INTEGER", nullable: true),
                    StationName = table.Column<string>(type: "TEXT", nullable: true),
                    StationLatitude = table.Column<double>(type: "REAL", nullable: true),
                    StationLongitude = table.Column<double>(type: "REAL", nullable: true),
                    LitersFilled = table.Column<double>(type: "REAL", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    ReceiptPhotoPath = table.Column<string>(type: "TEXT", nullable: true),
                    OdometerReading = table.Column<double>(type: "REAL", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmailSent = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FillUps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GpsRawPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Speed = table.Column<double>(type: "REAL", nullable: true),
                    Accuracy = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GpsRawPoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnownLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    RadiusMeters = table.Column<double>(type: "REAL", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnownLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RouteSegments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StartFillUpId = table.Column<int>(type: "INTEGER", nullable: true),
                    EndFillUpId = table.Column<int>(type: "INTEGER", nullable: true),
                    StartLocationName = table.Column<string>(type: "TEXT", nullable: false),
                    StartLatitude = table.Column<double>(type: "REAL", nullable: false),
                    StartLongitude = table.Column<double>(type: "REAL", nullable: false),
                    StartTimestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EndLocationName = table.Column<string>(type: "TEXT", nullable: false),
                    EndLatitude = table.Column<double>(type: "REAL", nullable: false),
                    EndLongitude = table.Column<double>(type: "REAL", nullable: false),
                    EndTimestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DistanceKm = table.Column<double>(type: "REAL", nullable: false),
                    PolylineEncoded = table.Column<string>(type: "TEXT", nullable: true),
                    Purpose = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteSegments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VehicleProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompanyName = table.Column<string>(type: "TEXT", nullable: false),
                    Cui = table.Column<string>(type: "TEXT", nullable: false),
                    DriverName = table.Column<string>(type: "TEXT", nullable: false),
                    VehiclePlate = table.Column<string>(type: "TEXT", nullable: false),
                    VehicleMakeModel = table.Column<string>(type: "TEXT", nullable: false),
                    VehicleCategory = table.Column<string>(type: "TEXT", nullable: false),
                    FuelType = table.Column<int>(type: "INTEGER", nullable: false),
                    FuelConsumptionNormPer100Km = table.Column<double>(type: "REAL", nullable: false),
                    GoogleMapsApiKey = table.Column<string>(type: "TEXT", nullable: false),
                    EmailRecipient = table.Column<string>(type: "TEXT", nullable: false),
                    EmailSubjectTemplate = table.Column<string>(type: "TEXT", nullable: false),
                    EmailBodyTemplate = table.Column<string>(type: "TEXT", nullable: false),
                    ReportingCadence = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleProfiles", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FillUps");

            migrationBuilder.DropTable(
                name: "GpsRawPoints");

            migrationBuilder.DropTable(
                name: "KnownLocations");

            migrationBuilder.DropTable(
                name: "RouteSegments");

            migrationBuilder.DropTable(
                name: "VehicleProfiles");
        }
    }
}
