﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MRIV.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaterialCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecipientId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    URL = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NotificationType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TitleTemplate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MessageTemplate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NotificationType = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Requisitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    PayrollNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IssueStationCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IssueStation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeliveryStationCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeliveryStation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DispatchType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DispatchPayrollNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DispatchVendor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CollectorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CollectorId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: true),
                    CompleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsExternal = table.Column<bool>(type: "bit", nullable: true),
                    ForwardToAdmin = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requisitions", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "WorkflowConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IssueStationCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeliveryStationCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaterialSubCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialCategoryId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialSubCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialSubCategories_MaterialCategories_MaterialCategoryId",
                        column: x => x.MaterialCategoryId,
                        principalTable: "MaterialCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowStepConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowConfigId = table.Column<int>(type: "int", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApproverRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RoleParameters = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false),
                    Conditions = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowStepConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowStepConfigs_WorkflowConfigs_WorkflowConfigId",
                        column: x => x.WorkflowConfigId,
                        principalTable: "WorkflowConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Materials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialCategoryId = table.Column<int>(type: "int", nullable: false),
                    MaterialSubcategoryId = table.Column<int>(type: "int", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StationCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Station = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    CurrentLocationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VendorId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Materials_MaterialCategories_MaterialCategoryId",
                        column: x => x.MaterialCategoryId,
                        principalTable: "MaterialCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Materials_MaterialSubCategories_MaterialSubcategoryId",
                        column: x => x.MaterialSubcategoryId,
                        principalTable: "MaterialSubCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Approvals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequisitionId = table.Column<int>(type: "int", nullable: false),
                    WorkflowConfigId = table.Column<int>(type: "int", nullable: true),
                    StepConfigId = table.Column<int>(type: "int", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    StepNumber = table.Column<int>(type: "int", nullable: false),
                    ApprovalStep = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PayrollNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsAutoGenerated = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Approvals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Approvals_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Approvals_WorkflowConfigs_WorkflowConfigId",
                        column: x => x.WorkflowConfigId,
                        principalTable: "WorkflowConfigs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Approvals_WorkflowStepConfigs_StepConfigId",
                        column: x => x.StepConfigId,
                        principalTable: "WorkflowStepConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RequisitionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequisitionId = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: true),
                    Condition = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SaveToInventory = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequisitionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequisitionItems_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequisitionItems_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaterialCondition",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialId = table.Column<int>(type: "int", nullable: true),
                    RequisitionId = table.Column<int>(type: "int", nullable: false),
                    RequisitionItemId = table.Column<int>(type: "int", nullable: true),
                    ApprovalId = table.Column<int>(type: "int", nullable: true),
                    Stage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Condition = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InspectedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InspectionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PhotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialCondition", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialCondition_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialCondition_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialCondition_RequisitionItems_RequisitionItemId",
                        column: x => x.RequisitionItemId,
                        principalTable: "RequisitionItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialCondition_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_RequisitionId",
                table: "Approvals",
                column: "RequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_StepConfigId",
                table: "Approvals",
                column: "StepConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_WorkflowConfigId",
                table: "Approvals",
                column: "WorkflowConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialCondition_ApprovalId",
                table: "MaterialCondition",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialCondition_MaterialId",
                table: "MaterialCondition",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialCondition_RequisitionId",
                table: "MaterialCondition",
                column: "RequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialCondition_RequisitionItemId",
                table: "MaterialCondition",
                column: "RequisitionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_MaterialCategoryId",
                table: "Materials",
                column: "MaterialCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_MaterialSubcategoryId",
                table: "Materials",
                column: "MaterialSubcategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialSubCategories_MaterialCategoryId",
                table: "MaterialSubCategories",
                column: "MaterialCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionItems_MaterialId",
                table: "RequisitionItems",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionItems_RequisitionId",
                table: "RequisitionItems",
                column: "RequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepConfigs_WorkflowConfigId",
                table: "WorkflowStepConfigs",
                column: "WorkflowConfigId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterialCondition");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "NotificationTemplates");

            migrationBuilder.DropTable(
                name: "StationCategories");

            migrationBuilder.DropTable(
                name: "Approvals");

            migrationBuilder.DropTable(
                name: "RequisitionItems");

            migrationBuilder.DropTable(
                name: "WorkflowStepConfigs");

            migrationBuilder.DropTable(
                name: "Materials");

            migrationBuilder.DropTable(
                name: "Requisitions");

            migrationBuilder.DropTable(
                name: "WorkflowConfigs");

            migrationBuilder.DropTable(
                name: "MaterialSubCategories");

            migrationBuilder.DropTable(
                name: "MaterialCategories");
        }
    }
}
