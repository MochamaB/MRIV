using MRIV.Enums;
using System;
using System.Collections.Generic;

namespace MRIV.ViewModels
{
    // Base dashboard view model (now renamed to MyRequisitionsDashboardViewModel)
    public class MyRequisitionsDashboardViewModel
    {
        // Basic metrics for all users
        public int TotalRequisitions { get; set; }
        public int PendingRequisitions { get; set; }
        public int CompletedRequisitions { get; set; }
        public int CancelledRequisitions { get; set; }
        
        // Status distribution for chart
        public Dictionary<string, int> RequisitionStatusCounts { get; set; } = new Dictionary<string, int>();
        
        // Recent requisitions
        public List<RequisitionSummary> RecentRequisitions { get; set; } = new List<RequisitionSummary>();
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

    public class RequisitionSummary
    {
        public int Id { get; set; }
        public string IssueStation { get; set; }
        public string DeliveryStation { get; set; }
        public RequisitionStatus Status { get; set; }
        public string StatusDescription { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int ItemCount { get; set; }
        public string PayrollNo { get; set; }
        public string EmployeeName { get; set; }
    }
}
