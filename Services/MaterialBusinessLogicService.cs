using Microsoft.Extensions.Configuration;
using MRIV.Enums;
using MRIV.Models;
using MRIV.ViewModels;

namespace MRIV.Services
{
    public interface IMaterialBusinessLogicService
    {
        string GetWarrantyStatus(DateTime? warrantyEndDate);
        string GetMaintenanceStatus(DateTime? nextMaintenanceDate);
        string GetAgeCategory(int? ageInDays);
        string GetValueCategory(decimal? purchasePrice);
        int GetUtilizationScore(string currentStatus, int? daysInCurrentStatus, int recentMovements);
        bool IsAvailable(MaterialStatus? materialStatus, string currentStatus);
        bool HasWarrantyAlert(DateTime? warrantyEndDate);
        bool HasMaintenanceAlert(DateTime? nextMaintenanceDate);
        bool HasUnderutilizedAlert(string currentStatus, int? daysInCurrentStatus, decimal? purchasePrice);
        string GetStatusDisplayName(MaterialStatus? status);
        string GetStatusColor(MaterialStatus? status);
        string GetConditionDisplayName(MaterialConditionStatus? condition);

        // Methods to interpret raw summary data
        MaterialStatusSummaryViewModel InterpretStatusCounts(Dictionary<int, int> statusCounts);
        MaterialValueCategoriesViewModel CategorizeValues(decimal totalValue, decimal averageValue, decimal maxValue, int totalCount);
        MaterialAlertSummaryViewModel CalculateAlerts(List<DateTime?> warrantyDates, List<DateTime?> maintenanceDates, List<MaterialUtilizationData> utilizationData);
    }

    public class MaterialBusinessLogicService : IMaterialBusinessLogicService
    {
        private readonly MaterialBusinessRulesConfig _config;

        public MaterialBusinessLogicService(IConfiguration configuration)
        {
            // Load business rules from configuration - makes them easily changeable
            _config = new MaterialBusinessRulesConfig
            {
                WarrantyAlertDays = configuration.GetValue<int>("MaterialRules:WarrantyAlertDays", 30),
                MaintenanceAlertDays = configuration.GetValue<int>("MaterialRules:MaintenanceAlertDays", 7),
                UnderutilizedDays = configuration.GetValue<int>("MaterialRules:UnderutilizedDays", 30),
                HighValueThreshold = configuration.GetValue<decimal>("MaterialRules:HighValueThreshold", 100000),
                MediumValueThreshold = configuration.GetValue<decimal>("MaterialRules:MediumValueThreshold", 10000),
                NewAssetMonths = configuration.GetValue<int>("MaterialRules:NewAssetMonths", 12),
                ModerateAssetYears = configuration.GetValue<int>("MaterialRules:ModerateAssetYears", 3)
            };
        }

        public string GetWarrantyStatus(DateTime? warrantyEndDate)
        {
            if (warrantyEndDate == null)
                return "No Warranty";

            var today = DateTime.Now;
            var daysUntilExpiry = (warrantyEndDate.Value - today).Days;

            if (daysUntilExpiry < 0)
                return "Expired";

            if (daysUntilExpiry <= _config.WarrantyAlertDays)
                return "Expiring Soon";

            return "Active";
        }

        public string GetMaintenanceStatus(DateTime? nextMaintenanceDate)
        {
            if (nextMaintenanceDate == null)
                return "No Schedule";

            var today = DateTime.Now;
            var daysUntilMaintenance = (nextMaintenanceDate.Value - today).Days;

            return daysUntilMaintenance switch
            {
                < 0 => "Overdue",
                var d when d <= _config.MaintenanceAlertDays => "Due Soon",
                _ => "Scheduled"
            };
        }

        public string GetAgeCategory(int? ageInDays)
        {
            if (ageInDays == null)
                return "Unknown";

            var ageInMonths = ageInDays.Value / 30.44; // Average days per month

            return ageInMonths switch
            {
                var m when m < _config.NewAssetMonths => "New",
                var m when m <= (_config.ModerateAssetYears * 12) => "Moderate",
                _ => "Old"
            };
        }

        public string GetValueCategory(decimal? purchasePrice)
        {
            var price = purchasePrice ?? 0;

            return price switch
            {
                0 => "No Value",
                <= 0 => "No Value",
                var p when p < _config.MediumValueThreshold => "Low Value",
                var p when p < _config.HighValueThreshold => "Medium Value",
                _ => "High Value"
            };
        }

        public int GetUtilizationScore(string currentStatus, int? daysInCurrentStatus, int recentMovements)
        {
            return currentStatus switch
            {
                "Assigned" => 100,
                "Recently_Returned" when daysInCurrentStatus <= 7 => 80,
                _ => recentMovements switch
                {
                    > 2 => 60,
                    > 0 => 40,
                    _ => 20
                }
            };
        }

        public bool IsAvailable(MaterialStatus? materialStatus, string currentStatus)
        {
            // Material is available if:
            // 1. Not currently assigned AND
            // 2. Not in a unavailable status (maintenance, lost, disposed)

            if (currentStatus == "Assigned")
                return false;

            return materialStatus switch
            {
                MaterialStatus.UnderMaintenance => false,
                MaterialStatus.LostOrStolen => false,
                MaterialStatus.Disposed => false,
                _ => true
            };
        }

        public bool HasWarrantyAlert(DateTime? warrantyEndDate)
        {
            if (warrantyEndDate == null)
                return false;

            var today = DateTime.Now;
            var daysUntilExpiry = (warrantyEndDate.Value - today).Days;

            return daysUntilExpiry > 0 && daysUntilExpiry <= _config.WarrantyAlertDays;
        }

        public bool HasMaintenanceAlert(DateTime? nextMaintenanceDate)
        {
            if (nextMaintenanceDate == null)
                return false;

            var today = DateTime.Now;
            return nextMaintenanceDate.Value <= today.AddDays(_config.MaintenanceAlertDays);
        }

        public bool HasUnderutilizedAlert(string currentStatus, int? daysInCurrentStatus, decimal? purchasePrice)
        {
            return currentStatus == "Available" &&
                   daysInCurrentStatus > _config.UnderutilizedDays &&
                   (purchasePrice ?? 0) > 50000; // Could make this configurable too
        }

        public string GetStatusDisplayName(MaterialStatus? status)
        {
            return status switch
            {
                MaterialStatus.Available => "Available",
                MaterialStatus.Assigned => "Assigned",
                MaterialStatus.UnderMaintenance => "Under Maintenance",
                MaterialStatus.LostOrStolen => "Lost/Stolen",
                MaterialStatus.Disposed => "Disposed",
                MaterialStatus.InProcess => "In Process",
                null => "Unknown",
                _ => status.ToString()
            };
        }

        public string GetStatusColor(MaterialStatus? status)
        {
            return status switch
            {
                MaterialStatus.Available => "success",      // Green
                MaterialStatus.Assigned => "primary",       // Blue
                MaterialStatus.UnderMaintenance => "warning", // Yellow
                MaterialStatus.LostOrStolen => "danger",    // Red
                MaterialStatus.Disposed => "secondary",     // Gray
                MaterialStatus.InProcess => "info",         // Light Blue
                _ => "light"                                 // Default
            };
        }

        public string GetConditionDisplayName(MaterialConditionStatus? condition)
        {
            return condition switch
            {
                MaterialConditionStatus.GoodCondition => "Good Condition",
                MaterialConditionStatus.Faulty=> "Faulty",
                MaterialConditionStatus.MinorDamage => "Minor Damage",
                MaterialConditionStatus.MajorDamage => " Major Damage",
                MaterialConditionStatus.UnderMaintenance => "Under Repair",
                MaterialConditionStatus.LostOrStolen => "Lost or Stolen",
                MaterialConditionStatus.Disposed => "Disposed",
                null => "Not Assessed",
                _ => condition.ToString()
            };
        }

        // Interpret raw MaterialStatus counts from database
        public MaterialStatusSummaryViewModel InterpretStatusCounts(Dictionary<int, int> statusCounts)
        {
            var summary = new MaterialStatusSummaryViewModel();

            foreach (var (statusValue, count) in statusCounts)
            {
                var status = (MaterialStatus)statusValue;
                var statusInfo = new StatusCountInfo
                {
                    Status = status,
                    Count = count,
                    DisplayName = GetStatusDisplayName(status),
                    Color = GetStatusColor(status)
                };

                switch (status)
                {
                    case MaterialStatus.Available:
                        summary.Available = statusInfo;
                        break;
                    case MaterialStatus.Assigned:
                        summary.Assigned = statusInfo;
                        break;
                    case MaterialStatus.UnderMaintenance:
                        summary.UnderMaintenance = statusInfo;
                        break;
                    case MaterialStatus.LostOrStolen:
                        summary.LostOrStolen = statusInfo;
                        break;
                    case MaterialStatus.Disposed:
                        summary.Disposed = statusInfo;
                        break;
                    case MaterialStatus.InProcess:
                        summary.InProcess = statusInfo;
                        break;
                }
            }

            return summary;
        }

        // Categorize values based on configured thresholds
        public MaterialValueCategoriesViewModel CategorizeValues(decimal totalValue, decimal averageValue, decimal maxValue, int totalCount)
        {
            return new MaterialValueCategoriesViewModel
            {
                TotalValue = totalValue,
                AverageValue = averageValue,
                MaxValue = maxValue,
                TotalCount = totalCount,
                HighValueThreshold = _config.HighValueThreshold,
                MediumValueThreshold = _config.MediumValueThreshold,
                HighValuePercentage = totalCount > 0 ? Math.Round((decimal)100 * (maxValue > _config.HighValueThreshold ? 1 : 0), 2) : 0
            };
        }

        // Calculate alerts from raw date data
        public MaterialAlertSummaryViewModel CalculateAlerts(List<DateTime?> warrantyDates, List<DateTime?> maintenanceDates, List<MaterialUtilizationData> utilizationData)
        {
            var alerts = new MaterialAlertSummaryViewModel();

            // Calculate warranty alerts
            alerts.WarrantyExpiring = warrantyDates.Count(date => HasWarrantyAlert(date));
            alerts.WarrantyExpired = warrantyDates.Count(date => date.HasValue && date.Value < DateTime.Now);

            // Calculate maintenance alerts
            alerts.MaintenanceOverdue = maintenanceDates.Count(date => HasMaintenanceAlert(date) && date.HasValue && date.Value < DateTime.Now);
            alerts.MaintenanceDueSoon = maintenanceDates.Count(date => HasMaintenanceAlert(date) && date.HasValue && date.Value >= DateTime.Now);

            // Calculate underutilized assets
            alerts.UnderutilizedAssets = utilizationData.Count(data =>
                HasUnderutilizedAlert(data.CurrentStatus, data.DaysInCurrentStatus, data.PurchasePrice));

            return alerts;
        }
    }

    // Configuration class for business rules
    public class MaterialBusinessRulesConfig
    {
        public int WarrantyAlertDays { get; set; } = 30;
        public int MaintenanceAlertDays { get; set; } = 7;
        public int UnderutilizedDays { get; set; } = 30;
        public decimal HighValueThreshold { get; set; } = 100000;
        public decimal MediumValueThreshold { get; set; } = 10000;
        public int NewAssetMonths { get; set; } = 12;
        public int ModerateAssetYears { get; set; } = 3;
    }

    // Supporting ViewModels for business logic processing
    public class MaterialStatusSummaryViewModel
    {
        public StatusCountInfo Available { get; set; } = new();
        public StatusCountInfo Assigned { get; set; } = new();
        public StatusCountInfo UnderMaintenance { get; set; } = new();
        public StatusCountInfo LostOrStolen { get; set; } = new();
        public StatusCountInfo Disposed { get; set; } = new();
        public StatusCountInfo InProcess { get; set; } = new();
    }

    public class StatusCountInfo
    {
        public MaterialStatus Status { get; set; }
        public int Count { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }

    public class MaterialValueCategoriesViewModel
    {
        public decimal TotalValue { get; set; }
        public decimal AverageValue { get; set; }
        public decimal MaxValue { get; set; }
        public int TotalCount { get; set; }
        public decimal HighValueThreshold { get; set; }
        public decimal MediumValueThreshold { get; set; }
        public decimal HighValuePercentage { get; set; }
    }

    public class MaterialAlertSummaryViewModel
    {
        public int WarrantyExpiring { get; set; }
        public int WarrantyExpired { get; set; }
        public int MaintenanceOverdue { get; set; }
        public int MaintenanceDueSoon { get; set; }
        public int UnderutilizedAssets { get; set; }
        public int TotalAlerts => WarrantyExpiring + MaintenanceOverdue + MaintenanceDueSoon + UnderutilizedAssets;
    }

    public class MaterialUtilizationData
    {
        public string CurrentStatus { get; set; } = string.Empty;
        public int? DaysInCurrentStatus { get; set; }
        public decimal? PurchasePrice { get; set; }
    }
}