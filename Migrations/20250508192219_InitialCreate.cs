using System;
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
                name: "RoleGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CanAccessAcrossStations = table.Column<bool>(type: "bit", nullable: false),
                    CanAccessAcrossDepartments = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleGroups", x => x.Id);
                });

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
                name: "RoleGroupMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleGroupId = table.Column<int>(type: "int", nullable: false),
                    PayrollNo = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleGroupMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleGroupMembers_RoleGroups_RoleGroupId",
                        column: x => x.RoleGroupId,
                        principalTable: "RoleGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    ModuleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    ValidationRules = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                name: "WorkflowStepConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowConfigId = table.Column<int>(type: "int", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StepAction = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    VendorId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PurchasePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    WarrantyStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WarrantyEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WarrantyTerms = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExpectedLifespanMonths = table.Column<int>(type: "int", nullable: true),
                    MaintenanceIntervalMonths = table.Column<int>(type: "int", nullable: true),
                    LastMaintenanceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextMaintenanceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Manufacturer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModelNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    QRCODE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AssetTag = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Specifications = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                name: "MediaFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Collection = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ModelType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ModelId = table.Column<int>(type: "int", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Alt = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CustomProperties = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaterialSubcategoryId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaFiles_MaterialSubCategories_MaterialSubcategoryId",
                        column: x => x.MaterialSubcategoryId,
                        principalTable: "MaterialSubCategories",
                        principalColumn: "Id");
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

            migrationBuilder.CreateTable(
                name: "Approvals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequisitionId = table.Column<int>(type: "int", nullable: false),
                    WorkflowConfigId = table.Column<int>(type: "int", nullable: true),
                    StepConfigId = table.Column<int>(type: "int", nullable: true),
                    StationId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    StepNumber = table.Column<int>(type: "int", nullable: false),
                    ApprovalStep = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApprovalAction = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PayrollNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsAutoGenerated = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Approvals", x => x.Id);
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
                name: "MaterialAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    PayrollNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AssignmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StationCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StationId = table.Column<int>(type: "int", maxLength: 100, nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    SpecificLocation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AssignmentType = table.Column<int>(type: "int", nullable: false),
                    RequisitionId = table.Column<int>(type: "int", nullable: true),
                    AssignedByPayrollNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialAssignments_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    RequisitionType = table.Column<int>(type: "int", nullable: false),
                    IssueStationCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IssueStationId = table.Column<int>(type: "int", nullable: false),
                    IssueDepartmentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeliveryStationCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeliveryStationId = table.Column<int>(type: "int", nullable: false),
                    DeliveryDepartmentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    MaterialAssignmentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Requisitions_MaterialAssignments_MaterialAssignmentId",
                        column: x => x.MaterialAssignmentId,
                        principalTable: "MaterialAssignments",
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
                name: "MaterialConditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialId = table.Column<int>(type: "int", nullable: true),
                    MaterialAssignmentId = table.Column<int>(type: "int", nullable: true),
                    RequisitionId = table.Column<int>(type: "int", nullable: true),
                    RequisitionItemId = table.Column<int>(type: "int", nullable: true),
                    ApprovalId = table.Column<int>(type: "int", nullable: true),
                    ConditionCheckType = table.Column<int>(type: "int", nullable: true),
                    Stage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Condition = table.Column<int>(type: "int", nullable: true),
                    FunctionalStatus = table.Column<int>(type: "int", nullable: true),
                    CosmeticStatus = table.Column<int>(type: "int", nullable: true),
                    ComponentStatuses = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InspectedBy = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    InspectionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActionRequired = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ActionDueDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialConditions_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialConditions_MaterialAssignments_MaterialAssignmentId",
                        column: x => x.MaterialAssignmentId,
                        principalTable: "MaterialAssignments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialConditions_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialConditions_RequisitionItems_RequisitionItemId",
                        column: x => x.RequisitionItemId,
                        principalTable: "RequisitionItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialConditions_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "Id");
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
                name: "IX_MaterialAssignments_MaterialId",
                table: "MaterialAssignments",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialAssignments_RequisitionId",
                table: "MaterialAssignments",
                column: "RequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialConditions_ApprovalId",
                table: "MaterialConditions",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialConditions_MaterialAssignmentId",
                table: "MaterialConditions",
                column: "MaterialAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialConditions_MaterialId",
                table: "MaterialConditions",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialConditions_RequisitionId",
                table: "MaterialConditions",
                column: "RequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialConditions_RequisitionItemId",
                table: "MaterialConditions",
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
                name: "IX_MediaFiles_MaterialSubcategoryId",
                table: "MediaFiles",
                column: "MaterialSubcategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_ModelType_ModelId",
                table: "MediaFiles",
                columns: new[] { "ModelType", "ModelId" });

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionItems_MaterialId",
                table: "RequisitionItems",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionItems_RequisitionId",
                table: "RequisitionItems",
                column: "RequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Requisitions_MaterialAssignmentId",
                table: "Requisitions",
                column: "MaterialAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleGroupMembers_RoleGroupId",
                table: "RoleGroupMembers",
                column: "RoleGroupId");

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

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepConfigs_WorkflowConfigId",
                table: "WorkflowStepConfigs",
                column: "WorkflowConfigId");

            migrationBuilder.AddForeignKey(
                name: "FK_Approvals_Requisitions_RequisitionId",
                table: "Approvals",
                column: "RequisitionId",
                principalTable: "Requisitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialAssignments_Requisitions_RequisitionId",
                table: "MaterialAssignments",
                column: "RequisitionId",
                principalTable: "Requisitions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialAssignments_Requisitions_RequisitionId",
                table: "MaterialAssignments");

            migrationBuilder.DropTable(
                name: "MaterialConditions");

            migrationBuilder.DropTable(
                name: "MediaFiles");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "NotificationTemplates");

            migrationBuilder.DropTable(
                name: "RoleGroupMembers");

            migrationBuilder.DropTable(
                name: "SettingValues");

            migrationBuilder.DropTable(
                name: "StationCategories");

            migrationBuilder.DropTable(
                name: "Approvals");

            migrationBuilder.DropTable(
                name: "RequisitionItems");

            migrationBuilder.DropTable(
                name: "RoleGroups");

            migrationBuilder.DropTable(
                name: "SettingDefinitions");

            migrationBuilder.DropTable(
                name: "WorkflowStepConfigs");

            migrationBuilder.DropTable(
                name: "SettingGroups");

            migrationBuilder.DropTable(
                name: "WorkflowConfigs");

            migrationBuilder.DropTable(
                name: "Requisitions");

            migrationBuilder.DropTable(
                name: "MaterialAssignments");

            migrationBuilder.DropTable(
                name: "Materials");

            migrationBuilder.DropTable(
                name: "MaterialSubCategories");

            migrationBuilder.DropTable(
                name: "MaterialCategories");
        }
    }
}
