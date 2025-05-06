using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MRIV.Migrations
{
    /// <inheritdoc />
    public partial class SettingsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SettingGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    ParentGroupId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettingGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SettingGroups_SettingGroups_ParentGroupId",
                        column: x => x.ParentGroupId,
                        principalTable: "SettingGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SettingDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    DefaultValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsUserConfigurable = table.Column<bool>(type: "bit", nullable: false),
                    ModuleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    ValidationRules = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettingDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SettingDefinitions_SettingGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "SettingGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SettingValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SettingDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    ScopeId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettingValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SettingValues_SettingDefinitions_SettingDefinitionId",
                        column: x => x.SettingDefinitionId,
                        principalTable: "SettingDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SettingDefinitions_GroupId",
                table: "SettingDefinitions",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_SettingDefinitions_Key",
                table: "SettingDefinitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SettingGroups_ParentGroupId",
                table: "SettingGroups",
                column: "ParentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_SettingValues_SettingDefinitionId_Scope_ScopeId",
                table: "SettingValues",
                columns: new[] { "SettingDefinitionId", "Scope", "ScopeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SettingValues");

            migrationBuilder.DropTable(
                name: "SettingDefinitions");

            migrationBuilder.DropTable(
                name: "SettingGroups");
        }
    }
}
