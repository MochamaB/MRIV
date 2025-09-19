using MRIV.Enums;
using MRIV.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MRIV.ViewModels
{
    // Enhanced dashboard view model with UserProfile integration
    public class MyRequisitionsDashboardViewModel
    {
        // User Information
        public UserDashboardInfo UserInfo { get; set; } = new();

        // Basic metrics for all users (existing)
        public int TotalRequisitions { get; set; }
        public int PendingRequisitions { get; set; }
        public int CompletedRequisitions { get; set; }
        public int CancelledRequisitions { get; set; }

        // New primary metrics
        public int AwaitingMyAction { get; set; }
        public int ThisMonthRequisitions { get; set; }
        public double AverageProcessingDays { get; set; }
        public int OverdueRequisitions { get; set; }

        // Trend data (real calculations)
        public decimal TotalRequisitionsTrend { get; set; }
        public decimal PendingRequisitionsTrend { get; set; }
        public decimal CompletedRequisitionsTrend { get; set; }
        public decimal ThisMonthTrend { get; set; }

        // Status distribution for chart (existing)
        public Dictionary<string, int> RequisitionStatusCounts { get; set; } = new Dictionary<string, int>();

        // Recent requisitions (existing)
        public List<RequisitionSummary> RecentRequisitions { get; set; } = new List<RequisitionSummary>();

        // New collections
        public ActionRequiredSection ActionRequired { get; set; } = new();
        public TrendAnalysisData TrendData { get; set; } = new();
        public QuickStatsData QuickStats { get; set; } = new();

        // Permission context
        public bool CanAccessDepartmentData { get; set; }
        public bool CanAccessStationData { get; set; }
       // public PermissionLevel PermissionLevel { get; set; }

        // Error handling
        public string ErrorMessage { get; set; }

        // Computed properties for optimization
        [JsonIgnore]
        public bool HasData => TotalRequisitions > 0;

        [JsonIgnore]
        public bool HasActionItems => AwaitingMyAction > 0 || OverdueRequisitions > 0;

        [JsonIgnore]
        public bool HasRecentActivity => RecentRequisitions?.Any() == true;

        [JsonIgnore]
        public string FormattedAverageProcessingDays => AverageProcessingDays > 0 ?
            $"{AverageProcessingDays:F1} days" : "N/A";

        [JsonIgnore]
        public decimal CompletionRate => TotalRequisitions > 0 ?
            Math.Round((decimal)CompletedRequisitions / TotalRequisitions * 100, 1) : 0;

        // Performance indicators
        [JsonIgnore]
        public string PerformanceStatus
        {
            get
            {
                if (CompletionRate >= 90) return "Excellent";
                if (CompletionRate >= 75) return "Good";
                if (CompletionRate >= 50) return "Average";
                return "Needs Improvement";
            }
        }

        [JsonIgnore]
        public string PerformanceColor
        {
            get
            {
                if (CompletionRate >= 90) return "success";
                if (CompletionRate >= 75) return "info";
                if (CompletionRate >= 50) return "warning";
                return "danger";
            }
        }
    }

    // Department dashboard view model
    public class DepartmentDashboardViewModel
    {
        public string DepartmentName { get; set; }
        
        // Department metrics
        public int TotalDepartmentRequisitions { get; set; }
        public int PendingDepartmentRequisitions { get; set; }
        public int CompletedDepartmentRequisitions { get; set; }
        public int CancelledDepartmentRequisitions { get; set; }
        
        // Status distribution for chart
        public Dictionary<string, int> DepartmentRequisitionStatusCounts { get; set; } = new Dictionary<string, int>();
        
        // Recent department requisitions
        public List<RequisitionSummary> RecentDepartmentRequisitions { get; set; } = new List<RequisitionSummary>();
    }

    // Enhanced Requisition Summary with additional fields
    public class RequisitionSummary
    {
        public int Id { get; set; }
        // Keep original string properties for backward compatibility
        public string IssueStation { get; set; }
        public string DeliveryStation { get; set; }

        // Add new ID-based properties
        public int IssueStationId { get; set; }
        public string IssueDepartmentId { get; set; }
        public int DeliveryStationId { get; set; }
        public string DeliveryDepartmentId { get; set; }

        public RequisitionStatus Status { get; set; }
        public string StatusDescription { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int ItemCount { get; set; }
        public string PayrollNo { get; set; }
        public string EmployeeName { get; set; }

        // New enhanced fields
        public string Priority { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public int DaysInCurrentStatus { get; set; }
        public bool IsOverdue { get; set; }

        // Computed properties for UI optimization
        [JsonIgnore]
        public string StatusBadgeClass
        {
            get
            {
                return Status switch
                {
                    RequisitionStatus.NotStarted => "bg-warning",
                    RequisitionStatus.PendingDispatch => "bg-info",
                    RequisitionStatus.PendingReceipt => "bg-primary",
                    RequisitionStatus.Completed => "bg-success",
                    RequisitionStatus.Cancelled => "bg-danger",
                    _ => "bg-secondary"
                };
            }
        }

        [JsonIgnore]
        public string PriorityBadgeClass
        {
            get
            {
                return Priority?.ToLower() switch
                {
                    "high" => "bg-danger",
                    "medium" => "bg-warning",
                    "low" => "bg-info",
                    _ => "bg-secondary"
                };
            }
        }

        [JsonIgnore]
        public string FormattedCreatedDate => CreatedAt?.ToString("dd MMM yyyy") ?? "N/A";

        [JsonIgnore]
        public string FormattedExpectedDate => ExpectedDeliveryDate?.ToString("dd MMM yyyy") ?? "N/A";

        [JsonIgnore]
        public string UrgencyIndicator
        {
            get
            {
                if (IsOverdue) return "urgent";
                if (DaysInCurrentStatus > 5) return "attention";
                return "normal";
            }
        }

        [JsonIgnore]
        public string UrgencyColor
        {
            get
            {
                return UrgencyIndicator switch
                {
                    "urgent" => "text-danger",
                    "attention" => "text-warning",
                    _ => "text-muted"
                };
            }
        }
    }

    // User dashboard context information
    public class UserDashboardInfo
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Department { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Station { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PayrollNo { get; set; } = string.Empty;

        public DateTime LastLogin { get; set; }

        // Computed properties
        [JsonIgnore]
        public string FormattedLastLogin => LastLogin != DateTime.MinValue ?
            LastLogin.ToString("dd MMM yyyy HH:mm") : "Never";

        [JsonIgnore]
        public bool IsRecentLogin => LastLogin > DateTime.UtcNow.AddHours(-1);

        [JsonIgnore]
        public string LoginStatus => IsRecentLogin ? "Recently Active" : "Idle";
    }

    // Action required section data
    public class ActionRequiredSection
    {
        [Range(0, int.MaxValue)]
        public int PendingReceiptConfirmation { get; set; }

        [Range(0, int.MaxValue)]
        public int RequiringClarification { get; set; }

        [Range(0, int.MaxValue)]
        public int OverdueItems { get; set; }

        [Range(0, int.MaxValue)]
        public int ReadyForCollection { get; set; }

        public List<UrgentRequisition> UrgentRequisitions { get; set; } = new();

        // Computed properties
        [JsonIgnore]
        public int TotalActionItems => PendingReceiptConfirmation + RequiringClarification + OverdueItems + ReadyForCollection;

        [JsonIgnore]
        public bool HasCriticalItems => OverdueItems > 0;

        [JsonIgnore]
        public bool HasAnyActions => TotalActionItems > 0;

        [JsonIgnore]
        public string PriorityMessage
        {
            get
            {
                if (OverdueItems > 0) return $"{OverdueItems} overdue items need immediate attention";
                if (PendingReceiptConfirmation > 0) return $"{PendingReceiptConfirmation} items await your confirmation";
                if (RequiringClarification > 0) return $"{RequiringClarification} items need clarification";
                if (ReadyForCollection > 0) return $"{ReadyForCollection} items ready for collection";
                return "No pending actions";
            }
        }

        [JsonIgnore]
        public string AlertLevel
        {
            get
            {
                if (OverdueItems > 0) return "danger";
                if (PendingReceiptConfirmation > 3) return "warning";
                if (TotalActionItems > 0) return "info";
                return "success";
            }
        }
    }

    // Urgent requisition information
    public class UrgentRequisition
    {
        public int RequisitionId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int DaysOverdue { get; set; }
        public string ActionRequired { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
    }

    // Trend analysis data for charts
    public class TrendAnalysisData
    {
        public List<MonthlyRequisitionData> RequisitionsByMonth { get; set; } = new();
        public Dictionary<string, int> RequisitionsByCategory { get; set; } = new();
        public List<TopRequestedItem> TopRequestedItems { get; set; } = new();
        public Dictionary<string, int> StatusDistribution { get; set; } = new();
    }

    // Monthly requisition trend data
    public class MonthlyRequisitionData
    {
        public string Month { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Count { get; set; }
        public int Completed { get; set; }
        public int Pending { get; set; }
        public int Cancelled { get; set; }
    }

    // Top requested items data
    public class TopRequestedItem
    {
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int RequestCount { get; set; }
        public int TotalQuantity { get; set; }
    }

    // Quick stats for CRM-style widget
    public class QuickStatsData
    {
        public int CompletedThisMonth { get; set; }
        public double AverageItemsPerRequisition { get; set; }
        public string MostRequestedCategory { get; set; } = string.Empty;
        public double FastestCompletionDays { get; set; }
        public int TotalItemsRequested { get; set; }
        public int ActiveRequisitions { get; set; }

        // Computed properties for better display
        [JsonIgnore]
        public string FormattedAverageItems => AverageItemsPerRequisition > 0 ?
            $"{AverageItemsPerRequisition:F1} items/request" : "N/A";

        [JsonIgnore]
        public string FormattedFastestCompletion => FastestCompletionDays > 0 ?
            $"{FastestCompletionDays:F1} days" : "N/A";
    }

    #region Data Transfer Objects for API Optimization

    /// <summary>
    /// Lightweight DTO for dashboard API responses
    /// </summary>
    public class DashboardDataDto
    {
        public MetricsDto Metrics { get; set; } = new();
        public TrendsDto Trends { get; set; } = new();
        public QuickStatsDto QuickStats { get; set; } = new();
        public List<RequisitionSummaryDto> RecentRequisitions { get; set; } = new();
        public ActionSummaryDto ActionRequired { get; set; } = new();
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    public class MetricsDto
    {
        public int TotalRequisitions { get; set; }
        public int PendingRequisitions { get; set; }
        public int CompletedRequisitions { get; set; }
        public int AwaitingMyAction { get; set; }
        public int ThisMonthRequisitions { get; set; }
        public int OverdueRequisitions { get; set; }
        public double AverageProcessingDays { get; set; }
        public decimal CompletionRate { get; set; }
    }

    public class TrendsDto
    {
        public decimal TotalTrend { get; set; }
        public decimal PendingTrend { get; set; }
        public decimal CompletedTrend { get; set; }
        public decimal MonthlyTrend { get; set; }
        public List<MonthlyDataPoint> MonthlyData { get; set; } = new();
    }

    public class MonthlyDataPoint
    {
        public string Month { get; set; } = string.Empty;
        public int Count { get; set; }
        public int Completed { get; set; }
        public int Pending { get; set; }
    }

    public class QuickStatsDto
    {
        public int CompletedThisMonth { get; set; }
        public double AverageItemsPerRequisition { get; set; }
        public string MostRequestedCategory { get; set; } = string.Empty;
        public int TotalItemsRequested { get; set; }
        public int ActiveRequisitions { get; set; }
    }

    public class RequisitionSummaryDto
    {
        public int Id { get; set; }
        public string IssueStation { get; set; } = string.Empty;
        public string DeliveryStation { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusBadgeClass { get; set; } = string.Empty;
        public string CreatedDate { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string PriorityBadgeClass { get; set; } = string.Empty;
        public int DaysInStatus { get; set; }
        public bool IsOverdue { get; set; }
        public string UrgencyColor { get; set; } = string.Empty;
    }

    public class ActionSummaryDto
    {
        public int PendingReceipt { get; set; }
        public int NeedClarification { get; set; }
        public int Overdue { get; set; }
        public int ReadyForCollection { get; set; }
        public int TotalActions { get; set; }
        public string PriorityMessage { get; set; } = string.Empty;
        public string AlertLevel { get; set; } = string.Empty;
        public List<UrgentItemDto> UrgentItems { get; set; } = new();
    }

    public class UrgentItemDto
    {
        public int RequisitionId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int DaysOverdue { get; set; }
        public string ActionRequired { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
    }

    /// <summary>
    /// Chart data DTO for visualization APIs
    /// </summary>
    public class ChartDataDto
    {
        public Dictionary<string, int> StatusDistribution { get; set; } = new();
        public Dictionary<string, int> CategoryDistribution { get; set; } = new();
        public List<MonthlyDataPoint> TrendData { get; set; } = new();
        public List<TopItemDto> TopItems { get; set; } = new();
    }

    public class TopItemDto
    {
        public string ItemName { get; set; } = string.Empty;
        public int RequestCount { get; set; }
        public int TotalQuantity { get; set; }
    }

    #endregion

    /// <summary>
    /// Management Dashboard ViewModel - Role-based organizational insights
    /// Separate from MyRequisitionsDashboardViewModel which shows personal data
    /// </summary>
    public class ManagementDashboardViewModel
    {
        // User context and permissions
        public UserDashboardInfo UserInfo { get; set; } = new();
        public string DashboardTitle { get; set; } = string.Empty;
        public string DashboardContext { get; set; } = string.Empty;
        public string AccessLevel { get; set; } = string.Empty; // "Department", "Station", "Cross-Station", "Organization"

        // Permission-based scope metrics
        public ScopeMetrics PrimaryMetrics { get; set; } = new();
        public Dictionary<string, int> StatusDistribution { get; set; } = new();
        public List<TrendDataPoint> TrendAnalysis { get; set; } = new();

        // Comparison data (for managers who can see multiple entities)
        public List<ComparisonEntity> ComparisonData { get; set; } = new();
        public bool HasComparisonData => ComparisonData?.Any() == true;

        // Material data within user's scope
        public MaterialScopeData MaterialData { get; set; } = new();

        // Action items requiring management attention
        public List<ManagementActionItem> ActionRequired { get; set; } = new();

        // Recent organizational activity within scope
        public List<RecentOrganizationalActivity> RecentActivity { get; set; } = new();

        // Access control flags
        public bool IsDefaultUser { get; set; }
        public bool CanAccessManagementDashboard => !IsDefaultUser;

        // Error handling
        public string ErrorMessage { get; set; } = string.Empty;
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    }

    /// <summary>
    /// Metrics scoped to user's permission level
    /// </summary>
    public class ScopeMetrics
    {
        public int TotalRequisitions { get; set; }
        public int PendingActions { get; set; }
        public int ThisMonthActivity { get; set; }
        public decimal CompletionRate { get; set; }
        public double AverageProcessingTime { get; set; }
        public int OverdueItems { get; set; }

        // Computed properties
        public string FormattedCompletionRate => $"{CompletionRate:F1}%";
        public string FormattedProcessingTime => AverageProcessingTime > 0 ? $"{AverageProcessingTime:F1} days" : "N/A";
    }

    /// <summary>
    /// Comparison entity for multi-entity views (Station Managers, General Managers, Admins)
    /// </summary>
    public class ComparisonEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "Department" or "Station"
        public ScopeMetrics Metrics { get; set; } = new();
        public string PerformanceIndicator { get; set; } = string.Empty; // "Excellent", "Good", "Average", "Poor"
        public string PerformanceColor { get; set; } = string.Empty; // CSS class for performance indicator
    }

    /// <summary>
    /// Material data within user's access scope
    /// </summary>
    public class MaterialScopeData
    {
        public int TotalMaterials { get; set; }
        public decimal TotalValue { get; set; }
        public Dictionary<string, int> MaterialByCategory { get; set; } = new();
        public Dictionary<string, int> MaterialByCondition { get; set; } = new();
        public List<MaterialAlert> MaterialAlerts { get; set; } = new();

        // Computed properties
        public string FormattedTotalValue => TotalValue > 0 ? $"KSh {TotalValue:N0}" : "N/A";
        public bool HasMaterials => TotalMaterials > 0;
        public bool HasAlerts => MaterialAlerts?.Any() == true;
    }

    /// <summary>
    /// Material alerts for management attention
    /// </summary>
    public class MaterialAlert
    {
        public string Type { get; set; } = string.Empty; // "Low Stock", "Maintenance Due", "Warranty Expiring"
        public int Count { get; set; }
        public string Severity { get; set; } = string.Empty; // "Critical", "Warning", "Info"
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Management action items requiring attention
    /// </summary>
    public class ManagementActionItem
    {
        public string Type { get; set; } = string.Empty; // "Approval", "Review", "Escalation"
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Urgency { get; set; } = string.Empty; // "Critical", "High", "Medium", "Low"
        public string Priority { get; set; } = string.Empty; // "High", "Medium", "Low"
        public int Count { get; set; }
        public string ActionUrl { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string EntityName { get; set; } = string.Empty; // Department or Station name

        // Computed properties
        public string UrgencyClass => Urgency.ToLower() switch
        {
            "critical" => "text-danger",
            "high" => "text-warning",
            "medium" => "text-info",
            _ => "text-muted"
        };

        public bool IsOverdue => DueDate < DateTime.UtcNow;
    }

    /// <summary>
    /// Recent organizational activity within user's scope
    /// </summary>
    public class RecentOrganizationalActivity
    {
        public string Type { get; set; } = string.Empty; // "Requisition", "Approval", "Material"
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty; // Department or Station name
        public DateTime Timestamp { get; set; }
        public string ActorName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Station { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;

        // Computed properties
        public string FormattedTime => Timestamp.ToString("MMM dd, HH:mm");
        public string TimeAgo
        {
            get
            {
                var diff = DateTime.UtcNow - Timestamp;
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
                return $"{(int)diff.TotalDays}d ago";
            }
        }
    }

    /// <summary>
    /// Trend data point for time-series analysis
    /// </summary>
    public class TrendDataPoint
    {
        public string Period { get; set; } = string.Empty; // "Jan 2024", "Week 1", etc.
        public int Value { get; set; }
        public string Label { get; set; } = string.Empty;
        public Dictionary<string, int> SubValues { get; set; } = new(); // For breakdown data
    }

    // ===================================================================
    // MATERIAL DASHBOARD VIEWMODELS
    // ===================================================================

    /// <summary>
    /// Main Material Dashboard ViewModel
    /// </summary>
    public class MaterialDashboardViewModel
    {
        public UserProfile UserProfile { get; set; } = new();

        // Dashboard context properties required by controller
        public string DashboardTitle { get; set; } = "Material Dashboard";
        public string AccessLevel { get; set; } = string.Empty;
        public bool IsDefaultUser { get; set; } = false;
        public bool HasError { get; set; } = false;
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Material Dashboard KPI metrics
    /// </summary>
    public class MaterialKPIViewModel
    {
        public decimal TotalValue { get; set; }
        public string TotalValueFormatted { get; set; } = string.Empty;
        public decimal ValueTrend { get; set; }
        public int AvailableMaterials { get; set; }
        public int TotalMaterials { get; set; }
        public decimal UtilizationRate { get; set; }
        public string UtilizationStatus { get; set; } = string.Empty;
        public int MaintenanceAlerts { get; set; }
        public string AlertLevel { get; set; } = string.Empty;
    }

    /// <summary>
    /// Chart data for material distribution and status breakdown
    /// </summary>
    public class MaterialChartDataViewModel
    {
        public string Label { get; set; } = string.Empty;
        public int Value { get; set; }
        public decimal Percentage { get; set; }
        public string Color { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    /// <summary>
    /// Movement trend data for time-series charts
    /// </summary>
    public class MaterialMovementTrendViewModel
    {
        public string Period { get; set; } = string.Empty; // "Jan 2024", "Week 1"
        public int Assignments { get; set; }
        public int Returns { get; set; }
        public int Transfers { get; set; }
        public int NewAcquisitions { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }

    /// <summary>
    /// Material table data for detailed views
    /// </summary>
    public class MaterialTableDataViewModel
    {
        public int MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal? Value { get; set; }
        public string ValueFormatted { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
        public DateTime? AssignmentDate { get; set; }
        public DateTime? LastMaintenance { get; set; }
        public DateTime? NextMaintenance { get; set; }
        public string WarrantyStatus { get; set; } = string.Empty;
        public int? DaysIdle { get; set; }
        public string Suggestion { get; set; } = string.Empty;
        public string ActionRequired { get; set; } = string.Empty;
    }

    /// <summary>
    /// Material movement table data
    /// </summary>
    public class MaterialMovementTableViewModel
    {
        public DateTime MovementDate { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;
        public string MovementType { get; set; } = string.Empty;
        public string MovementTypeColor { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Material alerts and notifications
    /// </summary>
    public class MaterialAlertViewModel
    {
        public string Type { get; set; } = string.Empty; // "Warranty", "Maintenance", "Underutilized"
        public string Message { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty; // "High", "Medium", "Low"
        public string Color { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<string> MaterialNames { get; set; } = new();
        public string ActionUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Filter options for Material Dashboard
    /// </summary>
    public class MaterialFilterOptionsViewModel
    {
        public List<FilterOptionViewModel> Stations { get; set; } = new();
        public List<FilterOptionViewModel> Departments { get; set; } = new();
        public List<FilterOptionViewModel> Categories { get; set; } = new();
        public List<FilterOptionViewModel> StationCategories { get; set; } = new();
        public List<FilterOptionViewModel> StatusOptions { get; set; } = new();
        public List<FilterOptionViewModel> ValueRanges { get; set; } = new();
    }

    /// <summary>
    /// Generic filter option
    /// </summary>
    public class FilterOptionViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ParentId { get; set; } = string.Empty;
        public int Count { get; set; }
        public bool IsSelected { get; set; }
    }
}
