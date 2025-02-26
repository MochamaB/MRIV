using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MRIV.Migrations
{
    /// <inheritdoc />
    public partial class AddStationCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StationCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StationName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StationPoint = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DataSource = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FilterCriteria = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StationCategories", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "StationCategories",
                columns: new[] { "Id", "Code", "DataSource", "FilterCriteria", "StationName", "StationPoint" },
                values: new object[,]
                {
                    { 1, "headoffice", "Department", null, "Head Office", "both" },
                    { 2, "factory", "Station", "{\"exclude\": [\"region\", \"zonal\"]}", "Factory", "both" },
                    { 3, "region", "Station", "{\"include\": [\"region\"]}", "Region", "both" },
                    { 4, "vendor", "Vendor", null, "Vendor", "delivery" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StationCategories");
        }
    }
}
