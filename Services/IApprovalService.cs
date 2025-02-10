using MRIV.Models;
using Microsoft.EntityFrameworkCore;

namespace MRIV.Services
{
    public interface IApprovalService
    {
        Task<List<Approval>> CreateApprovalStepsAsync(Requisition requisition, EmployeeBkp employee);
    }
    public class ApprovalService : IApprovalService
    {
        private readonly RequisitionContext _context;
        private readonly IEmployeeService _employeeService;

        public ApprovalService(RequisitionContext context,
                              IEmployeeService employeeService)
        {
            _context = context;
            _employeeService = employeeService;
        }
        // Approval Rules Dictionary (Inside ApprovalService)
        private static readonly Dictionary<(string issue, string delivery), List<Func<Requisition, EmployeeBkp, Approval>>> ApprovalRules =
            new()
            {
                { ("headoffice", "headoffice"), new List<Func<Requisition, EmployeeBkp, Approval>>
                    {
                        (req, emp) => CreateStep("Supervisor Approval", emp?.PayrollNo),
                        (req, emp) => CreateStep("HO Employee Receipt", emp?.PayrollNo)
                    }
                },
                { ("headoffice", "factory"), new List<Func<Requisition, EmployeeBkp, Approval>>
                    {
                        (req, emp) => CreateStep("Supervisor Approval", emp?.PayrollNo),
                        (req, emp) => req.DispatchType == "admin"
                            ? CreateStep("Admin Dispatch Approval", req.DispatchPayrollNo ?? "Not Specified")
                            : CreateStep("Vendor Dispatch", req.DispatchVendor ?? "Not Specified"),
                        (req, emp) => CreateStep("Factory Employee Receipt", emp?.PayrollNo)
                    }
                },
                { ("headoffice", "region"), new List<Func<Requisition, EmployeeBkp, Approval>>
                    {
                        (req, emp) => CreateStep("Supervisor Approval", emp?.PayrollNo),
                        (req, emp) => req.DispatchType == "admin"
                            ? CreateStep("Admin Dispatch Approval", req.DispatchPayrollNo ?? "Not Specified")
                            : CreateStep("Vendor Dispatch", req.DispatchVendor ?? "Not Specified"),
                        (req, emp) => CreateStep("Region Employee Receipt", emp?.PayrollNo)
                    }
                },
                { ("headoffice", "vendor"), new List<Func<Requisition, EmployeeBkp, Approval>>
                    {
                        (req, emp) => CreateStep("Supervisor Approval", emp?.PayrollNo),
                        (req, emp) => req.DispatchType == "admin"
                            ? CreateStep("Admin Dispatch Approval", req.DispatchPayrollNo ?? "Not Specified")
                            : CreateStep("Vendor Dispatch", req.DispatchVendor ?? "Not Specified"),
                    }
                },

                { ("factory", "headoffice"), new List<Func<Requisition, EmployeeBkp, Approval>>
                    {
                        (req, emp) => CreateStep("Supervisor Approval", emp?.PayrollNo),
                        (req, emp) => CreateStep("HO Employee Receive", emp?.PayrollNo)
                    }
                },
                { ("factory", "factory"), new List<Func<Requisition, EmployeeBkp, Approval>>
                    {
                        (req, emp) => CreateStep("Supervisor Approval", emp?.PayrollNo),
                        (req, emp) => CreateStep("Factory Employee Receive", emp?.PayrollNo)
                    }
                },
                { ("factory", "region"), new List<Func<Requisition, EmployeeBkp, Approval>>
                    {
                        (req, emp) => CreateStep("Supervisor Approval", emp?.PayrollNo),
                        (req, emp) => CreateStep("Region Employee Receive", emp?.PayrollNo)
                    }
                },
                { ("factory", "vendor"), new List<Func<Requisition, EmployeeBkp, Approval>>
                    {
                        (req, emp) => CreateStep("Supervisor Approval", emp?.PayrollNo),
                   
                    }
                },

                 { ("region", "headoffice"), new List<Func<Requisition, EmployeeBkp, Approval>>
                    {
                        (req, emp) => CreateStep("Supervisor Approval", emp?.PayrollNo),
                        (req, emp) => CreateStep("HO Employee Receive", emp?.PayrollNo)
                    }
                },
                { ("region", "factory"), new List<Func<Requisition, EmployeeBkp, Approval>>
                    {
                        (req, emp) => CreateStep("Supervisor Approval", emp?.PayrollNo),
                        (req, emp) => CreateStep("Factory Employee Receive", emp?.PayrollNo)
                    }
                },
                { ("region", "region"), new List<Func<Requisition, EmployeeBkp, Approval>>
                    {
                        (req, emp) => CreateStep("Supervisor Approval", emp?.PayrollNo),
                        (req, emp) => CreateStep("Region Employee Receive", emp?.PayrollNo)
                    }
                },
                { ("region", "vendor"), new List<Func<Requisition, EmployeeBkp, Approval>>
                    {
                        (req, emp) => CreateStep("Supervisor Approval", emp?.PayrollNo),

                    }
                }
                // Add more rules as needed...
            };
        public async Task<List<Approval>> CreateApprovalStepsAsync(Requisition requisition, EmployeeBkp employee)
        {
            var approvalSteps = new List<Approval>();
           
            // Get required employees
            var supervisor = _employeeService.GetSupervisor(employee);
            var factoryEmployee = await _employeeService.GetFactoryEmployeeAsync(requisition.DeliveryStation);
            var regionEmployee = await _employeeService.GetRegionEmployee();
            var hoEmployee = await _employeeService.GetHoEmployeeAsync(requisition.DeliveryStation);

            string issue = requisition.IssueStationCategory;
            string delivery = requisition.DeliveryStationCategory;
            string dispatchType = requisition?.DispatchType ?? "Not Specified";

            // Define the employee mapping based on the station
            var employeeMapping = new Dictionary<string, EmployeeBkp>
                {
                    { "headoffice", hoEmployee },
                    { "factory", factoryEmployee },
                    { "region", regionEmployee }
                };

            // Check if the rule exists in the dictionary
            if (ApprovalRules.TryGetValue((requisition.IssueStation, requisition.DeliveryStation), out var steps))
            {
                foreach (var step in steps)
                {
                    approvalSteps.Add(step(requisition, employeeMapping.GetValueOrDefault(requisition.DeliveryStation)));
                }
            }

 
            // Add other combinations similarly (e.g., Head Office -> Region, etc.)

            // Assign sequential step numbers and statuses
            for (int i = 0; i < approvalSteps.Count; i++)
            {
                approvalSteps[i].ApprovalStatus = i == 0 ? "Pending" : "Not Started";
                approvalSteps[i].RequisitionId = requisition.Id;
                approvalSteps[i].DepartmentId = requisition.DepartmentId;
                approvalSteps[i].CreatedAt = DateTime.Now;
                approvalSteps[i].UpdatedAt = i == 0 ? DateTime.Now : null;
            }

            return approvalSteps;
        }

        private static Approval CreateStep(string stepName, string payrollNo)
        {
            return new Approval
            {
                ApprovalStep = stepName,
                PayrollNo = payrollNo // Ensure PayrollNo is a string in the Approval model
            };
        }

      


    }
}
