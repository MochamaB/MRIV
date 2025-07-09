using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MRIV.Models;
using MRIV.Services;

namespace MRIV.Services
{
    public interface INotificationManager
    {
        Task NotifyApprovalStepApproved(Approval approval, string approvedByPayrollNo, string nextStepName);
        Task NotifyApprovalStepRejected(Approval approval, string rejectedByPayrollNo, string reason);
        Task NotifyApprovalStepOnHold(Approval approval, string putOnHoldByPayrollNo, string reason);
        Task NotifyApprovalStepDispatched(Approval approval, string dispatcherPayrollNo, string deliveryStationName);
        Task NotifyApprovalStepReceived(Approval approval, string receiverPayrollNo, string deliveryStationName);
    }

    public class NotificationManager : INotificationManager
    {
        private readonly INotificationService _notificationService;
        private readonly IEmployeeService _employeeService;
        private readonly IDepartmentService _departmentService;
        private readonly ILocationService _locationService;

        public NotificationManager(INotificationService notificationService, IEmployeeService employeeService, IDepartmentService departmentService, ILocationService locationService)
        {
            _notificationService = notificationService;
            _employeeService = employeeService;
            _departmentService = departmentService;
            _locationService = locationService;
        }

        public async Task NotifyApprovalStepApproved(Approval approval, string approvedByPayrollNo, string nextStepName)
        {
            if (approval?.Requisition == null) return;
            var approver = await _employeeService.GetEmployeeByPayrollAsync(approvedByPayrollNo);
            var approverDept = approver != null ? await _departmentService.GetDepartmentByNameAsync(approver.Department) : null;
            string approverDisplay = approver != null
                ? $"{approver.Fullname} ({approverDept?.DepartmentName ?? "Unknown Department"})"
                : approvedByPayrollNo;
            string nextStep = nextStepName ?? "Completed";
            var creator = await _employeeService.GetEmployeeByPayrollAsync(approval.Requisition.PayrollNo);
            if (creator != null)
            {
                await _notificationService.CreateNotificationAsync(
                    "RequisitionApproved",
                    new Dictionary<string, string>
                    {
                        { "RequisitionId", approval.Requisition.Id.ToString() },
                        { "Approver", approverDisplay },
                        { "NextStep", nextStep }
                    },
                    creator.PayrollNo
                );
            }
        }

        public async Task NotifyApprovalStepRejected(Approval approval, string rejectedByPayrollNo, string reason)
        {
            if (approval?.Requisition == null) return;
            var approver = await _employeeService.GetEmployeeByPayrollAsync(rejectedByPayrollNo);
            var approverDept = approver != null ? await _departmentService.GetDepartmentByNameAsync(approver.Department) : null;
            string approverDisplay = approver != null
                ? $"{approver.Fullname} ({approverDept?.DepartmentName ?? "Unknown Department"})"
                : rejectedByPayrollNo;
            var creator = await _employeeService.GetEmployeeByPayrollAsync(approval.Requisition.PayrollNo);
            if (creator != null)
            {
                await _notificationService.CreateNotificationAsync(
                    "RequisitionRejected",
                    new Dictionary<string, string>
                    {
                        { "RequisitionId", approval.Requisition.Id.ToString() },
                        { "Approver", approverDisplay },
                        { "Reason", reason }
                    },
                    creator.PayrollNo
                );
            }
        }

        public async Task NotifyApprovalStepOnHold(Approval approval, string putOnHoldByPayrollNo, string reason)
        {
            if (approval?.Requisition == null) return;
            var approver = await _employeeService.GetEmployeeByPayrollAsync(putOnHoldByPayrollNo);
            var approverDept = approver != null ? await _departmentService.GetDepartmentByNameAsync(approver.Department) : null;
            string approverDisplay = approver != null
                ? $"{approver.Fullname} ({approverDept?.DepartmentName ?? "Unknown Department"})"
                : putOnHoldByPayrollNo;
            var creator = await _employeeService.GetEmployeeByPayrollAsync(approval.Requisition.PayrollNo);
            if (creator != null)
            {
                await _notificationService.CreateNotificationAsync(
                    "RequisitionOnHold",
                    new Dictionary<string, string>
                    {
                        { "RequisitionId", approval.Requisition.Id.ToString() },
                        { "Approver", approverDisplay },
                        { "Reason", reason }
                    },
                    creator.PayrollNo
                );
            }
        }

        public async Task NotifyApprovalStepDispatched(Approval approval, string dispatcherPayrollNo, string deliveryStationName)
        {
            if (approval?.Requisition == null) return;
            var dispatcher = await _employeeService.GetEmployeeByPayrollAsync(dispatcherPayrollNo);
            string dispatcherDisplay = dispatcher != null ? dispatcher.Fullname : dispatcherPayrollNo;
            var creator = await _employeeService.GetEmployeeByPayrollAsync(approval.Requisition.PayrollNo);
            if (creator != null)
            {
                await _notificationService.CreateNotificationAsync(
                    "ItemsDispatched",
                    new Dictionary<string, string>
                    {
                        { "RequisitionId", approval.Requisition.Id.ToString() },
                        { "Dispatcher", dispatcherDisplay },
                        { "DeliveryStation", deliveryStationName }
                    },
                    creator.PayrollNo
                );
            }
        }

        public async Task NotifyApprovalStepReceived(Approval approval, string receiverPayrollNo, string deliveryStationName)
        {
            if (approval?.Requisition == null) return;
            var receiver = await _employeeService.GetEmployeeByPayrollAsync(receiverPayrollNo);
            string receiverDisplay = receiver != null ? receiver.Fullname : receiverPayrollNo;
            var creator = await _employeeService.GetEmployeeByPayrollAsync(approval.Requisition.PayrollNo);
            if (creator != null)
            {
                await _notificationService.CreateNotificationAsync(
                    "ItemsReceived",
                    new Dictionary<string, string>
                    {
                        { "RequisitionId", approval.Requisition.Id.ToString() },
                        { "Receiver", receiverDisplay },
                        { "DeliveryStation", deliveryStationName }
                    },
                    creator.PayrollNo
                );
            }
        }
    }
} 