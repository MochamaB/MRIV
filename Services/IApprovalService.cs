using MRIV.Models;
using Microsoft.EntityFrameworkCore;

namespace MRIV.Services
{
    public interface IApprovalService
    {
        Task<List<Approval>> CreateApprovalStepsAsync(Requisition requisition, EmployeeBkp loggedInUserEmployee);
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
        private static readonly Dictionary<(string issue, string delivery), List<Func<Requisition, Dictionary<string, EmployeeBkp>, Approval>>> ApprovalRules =
            new()
            {
                { ("headoffice", "headoffice"), new List<Func<Requisition, Dictionary<string, EmployeeBkp>, Approval>>
                    {
                        (req, empMap) => CreateStep("Supervisor Approval", empMap["supervisor"]),
                        (req, empMap) => CreateStep("HO Employee Receipt", empMap["headoffice"])
                    }
             },
        // Update other rules ,
                { ("headoffice", "factory"),new List<Func<Requisition, Dictionary<string, EmployeeBkp>, Approval>>
                    {
                        (req, empMap) => CreateStep("Supervisor Approval",empMap["supervisor"]),
                        (req, empMap) => req.DispatchType == "admin"
                            ? CreateStep("Admin Dispatch Approval", empMap["dispatchAdmin"])
                            : CreateVendorStep("Vendor Dispatch", req.DispatchVendor ?? "Not Specified"),
                        (req, empMap) => CreateStep("Factory Employee Receipt", empMap["factory"])
                    }
                },
                { ("headoffice", "region"), new List<Func<Requisition, Dictionary<string, EmployeeBkp>, Approval>>
                    {
                        (req, empMap) => CreateStep("Supervisor Approval", empMap["supervisor"]),
                        (req, empMap) => req.DispatchType == "admin"
                            ? CreateStep("Admin Dispatch Approval", empMap["dispatchAdmin"])
                            : CreateVendorStep("Vendor Dispatch", req.DispatchVendor ?? "Not Specified"),
                        (req, empMap) => CreateStep("Region Employee Receipt",  empMap["region"])
                    }
                },
                { ("headoffice", "vendor"),new List<Func<Requisition, Dictionary<string, EmployeeBkp>, Approval>>
                    {
                        (req, empMap) => CreateStep("Supervisor Approval", empMap["supervisor"]),
                        (req, empMap) => req.DispatchType == "admin"
                            ? CreateStep("Admin Dispatch Approval", empMap["dispatchAdmin"])
                            : CreateVendorStep("Vendor Dispatch", req.DispatchVendor ?? "Not Specified"),
                    }
                },

                { ("factory", "headoffice"), new List<Func<Requisition, Dictionary<string, EmployeeBkp>, Approval>>
                    {
                        (req, empMap) => CreateStep("Supervisor Approval", empMap["supervisor"]),
                         (req, empMap) => CreateStep("HO Employee Receipt", empMap["headoffice"])
                    }
                },
                { ("factory", "factory"), new List<Func<Requisition, Dictionary<string, EmployeeBkp>, Approval>>
                    {
                        (req, empMap) => CreateStep("Supervisor Approval", empMap["supervisor"]),
                        (req, empMap) => CreateStep("Factory Employee Receipt", empMap["factory"])
                    }
                },
                { ("factory", "region"), new List<Func<Requisition, Dictionary<string, EmployeeBkp>, Approval>>
                    {
                        (req, empMap) => CreateStep("Supervisor Approval", empMap["supervisor"]),
                        (req, empMap) => CreateStep("Region Employee Receipt", empMap["region"])
                    }
                },
                { ("factory", "vendor"), new List<Func<Requisition, Dictionary<string, EmployeeBkp>, Approval>>
                    {
                         (req, empMap) => CreateStep("Supervisor Approval", empMap["supervisor"]),

                    }
                },

                 { ("region", "headoffice"), new List<Func<Requisition, Dictionary<string, EmployeeBkp>, Approval>>
                    {
                       (req, empMap) => CreateStep("Supervisor Approval", empMap["supervisor"]),
                          (req, empMap) => CreateStep("HO Employee Receipt", empMap["headoffice"])
                    }
                },
                { ("region", "factory"), new List<Func<Requisition, Dictionary<string, EmployeeBkp>, Approval>>
                    {
                        (req, empMap) => CreateStep("Supervisor Approval", empMap["supervisor"]),
                        (req, empMap) => CreateStep("Factory Employee Receipt", empMap["factory"])
                    }
                },
                { ("region", "region"), new List<Func<Requisition, Dictionary<string, EmployeeBkp>, Approval>>
                    {
                        (req, empMap) => CreateStep("Supervisor Approval", empMap["supervisor"]),
                        (req, empMap) => CreateStep("Region Employee Receipt", empMap["region"])
                    }
                },
                { ("region", "vendor"), new List<Func<Requisition, Dictionary<string, EmployeeBkp>, Approval>>
                    {
                         (req, empMap) => CreateStep("Supervisor Approval", empMap["supervisor"]),

                    }
                }
                // Add more rules as needed...
            };
        public async Task<List<Approval>> CreateApprovalStepsAsync(Requisition requisition, EmployeeBkp loggedInUserEmployee)
        {
            var approvalSteps = new List<Approval>();

            // Get required employees
            var supervisor = _employeeService.GetSupervisor(loggedInUserEmployee);
            var factoryEmployee = await _employeeService.GetFactoryEmployeeAsync(requisition.DeliveryStation);
            var regionEmployee = await _employeeService.GetRegionEmployee();
            var hoEmployee = await _employeeService.GetHoEmployeeAsync(requisition.DeliveryStation);

            string issue = requisition.IssueStationCategory;
            string delivery = requisition.DeliveryStationCategory;
            string dispatchType = requisition?.DispatchType ?? "Not Specified";

            // Define the employee mapping based on the station
            var employeeMapping = new Dictionary<string, EmployeeBkp>
                {
                    { "supervisor", supervisor },
                    { "headoffice", hoEmployee },
                    { "factory", factoryEmployee },
                    { "region", regionEmployee }
                };

            // Check if the rule exists in the dictionary
            if (ApprovalRules.TryGetValue((issue, delivery), out var steps))
            {
                foreach (var step in steps)
                {
                    // Pass the entire employeeMapping to the step
                    approvalSteps.Add(step(requisition, employeeMapping));
                }
            }
            else
            {
                Console.WriteLine("No matching approval rules found");
            }


            // Add other combinations similarly (e.g., Head Office -> Region, etc.)

            // Assign sequential step numbers and statuses
            for (int i = 0; i < approvalSteps.Count; i++)
            {
                approvalSteps[i].ApprovalStatus = i == 0 ? "Pending" : "Not Started";
                approvalSteps[i].RequisitionId = requisition.Id;
                approvalSteps[i].CreatedAt = DateTime.Now;
                approvalSteps[i].UpdatedAt = i == 0 ? DateTime.Now : null;
            }

            return approvalSteps;
        }

        private static Approval CreateStep(string stepName, EmployeeBkp employee)
        {
            return new Approval
            {
                ApprovalStep = stepName,
                PayrollNo = employee?.PayrollNo ?? "Not Specified",
                DepartmentId = string.IsNullOrEmpty(employee?.Department)
                            ? 0 // Default value for null or empty strings
                            : Convert.ToInt32(employee.Department) // Handle nulls as needed
            };
        }

        // Overload for vendors (no employee)
        private static Approval CreateVendorStep(string stepName, string vendorName)
        {
            return new Approval
            {
                ApprovalStep = stepName,
                PayrollNo = vendorName,
                DepartmentId = 0 // Or set to a default department for vendors
            };



        }
    }
}
