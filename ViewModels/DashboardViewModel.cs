using MRIV.Enums;
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
}
