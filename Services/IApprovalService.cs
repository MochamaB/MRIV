using MRIV.Models;
using Microsoft.EntityFrameworkCore;
using MRIV.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MRIV.Services
{
    public interface IApprovalService
    {
        Task<List<Approval>> CreateApprovalStepsAsync(Requisition requisition, EmployeeBkp loggedInUserEmployee);
        Task<List<ApprovalStepViewModel>> ConvertToViewModelsAsync(List<Approval> approvalSteps,Requisition requisition,List<Vendor> vendors);
        Task<Dictionary<string, SelectList>> PopulateDepartmentEmployeesAsync(Requisition requisition, List<Approval> approvalSteps);
    }
    public class ApprovalService : IApprovalService
    {
        private readonly RequisitionContext _context;
        private readonly IEmployeeService _employeeService;
        private readonly IDepartmentService _departmentService;
        private readonly VendorService _vendorService;

        public ApprovalService(RequisitionContext context,
                              IEmployeeService employeeService,IDepartmentService departmentService,VendorService vendorService)
        {
            _context = context;
            _employeeService = employeeService;
            _departmentService = departmentService;
            _vendorService = vendorService;
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

            // Add debug logs to check if factoryEmployee is valid
            Console.WriteLine($"Factory Employee Payroll: {factoryEmployee?.PayrollNo}");
            Console.WriteLine($"Factory Employee Department: {factoryEmployee?.Department}");

            // Fetch dispatch admin if DispatchType is "admin"
            EmployeeBkp? dispatchAdmin = null;
            if (requisition.DispatchType == "admin" && !string.IsNullOrEmpty(requisition.DispatchPayrollNo))
            {
                dispatchAdmin = await _employeeService.GetEmployeeByPayrollAsync(requisition.DispatchPayrollNo);
            }

            string issue = requisition.IssueStationCategory;
            string delivery = requisition.DeliveryStationCategory;
            string dispatchType = requisition?.DispatchType ?? "Not Specified";

            // Define the employee mapping based on the station
            var employeeMapping = new Dictionary<string, EmployeeBkp>
                {
                    { "supervisor", supervisor },
                    { "headoffice", hoEmployee },
                    { "factory", factoryEmployee },
                    { "region", regionEmployee },
                    { "dispatchAdmin", dispatchAdmin }
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
                approvalSteps[i].StepNumber = i + 1;
                approvalSteps[i].ApprovalStatus = i == 0 ? "Pending Approval" : "Not Started";
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
        // GET APPROVAl STEPS IN VIEW
        public async Task<List<ApprovalStepViewModel>> ConvertToViewModelsAsync(List<Approval> approvalSteps,Requisition requisition,List<Vendor> vendors)
        {
            var viewModels = new List<ApprovalStepViewModel>();

            foreach (var step in approvalSteps)
            {
                var viewModel = await ProcessStep(step, requisition, vendors);
                viewModels.Add(viewModel);
            }

            return viewModels;
        }

        private async Task<ApprovalStepViewModel> ProcessStep(Approval step,Requisition requisition,List<Vendor> vendors)
        {
            // Get department and employee information
            var department = await _departmentService.GetDepartmentByIdAsync(step.DepartmentId);
            var employee = await _employeeService.GetEmployeeByPayrollAsync(step.PayrollNo);

            string departmentName = department?.DepartmentName ?? "Unknown";
            string employeeName = employee?.Fullname ?? "Unknown";

            // Vendor Dispatch handling
            if (step.ApprovalStep == "Vendor Dispatch")
            {
                employeeName = GetVendorName(step.PayrollNo, vendors);
                departmentName = "N/A";
            }

            // Factory Employee Receipt handling
            if (step.ApprovalStep == "Factory Employee Receipt" && !string.IsNullOrEmpty(requisition.DeliveryStation))
            {
                var station = await _departmentService.GetStationByStationNameAsync(requisition.DeliveryStation);
                departmentName = $"{departmentName} ({station?.StationName ?? "Unknown Station"})";
            }
            // Factory Employee Receipt handling
            if (step.ApprovalStep == "HO Employee Receipt" && !string.IsNullOrEmpty(requisition.DeliveryStation))
            {
                var station = await _departmentService.GetDepartmentByNameAsync(requisition.DeliveryStation);
                departmentName = $"{departmentName}";
            }


            return new ApprovalStepViewModel
            {
                StepNumber = step.StepNumber,
                ApprovalStep = step.ApprovalStep,
                PayrollNo = step.PayrollNo,
                EmployeeName = employeeName,
                DepartmentId = step.DepartmentId,
                DepartmentName = departmentName,
                ApprovalStatus = step.ApprovalStatus,
                CreatedAt = step.CreatedAt
            };
        }

      
        private string GetVendorName(string payrollNo, List<Vendor> vendors)
        {
            if (int.TryParse(payrollNo, out int vendorId))
            {
                return vendors.FirstOrDefault(v => v.VendorID == vendorId)?.Name
                       ?? "Unknown Vendor";
            }
            return "Invalid Vendor ID";
        }

        public async Task<Dictionary<string, SelectList>> PopulateDepartmentEmployeesAsync(Requisition requisition, List<Approval> approvalSteps)
        {
            var departmentEmployees = new Dictionary<string, SelectList>();

            if (approvalSteps == null) return departmentEmployees;

            foreach (var step in approvalSteps)
            {
                // Get appropriate employees for this step
                var employees = await GetAppropriateEmployeesForStepAsync(step, requisition);

                // Create SelectList if we have valid employees
                if (employees != null && employees.Any())
                {
                    departmentEmployees[step.ApprovalStep] = new SelectList(
                        employees.Select(e => new {
                            PayrollNo = e.PayrollNo,
                            DisplayName = $"{e.Fullname} - {e.Designation}"
                        }),
                        "PayrollNo",
                        "DisplayName"
                    );
                }
                else
                {
                    // Add an empty SelectList to prevent null reference
                    departmentEmployees[step.ApprovalStep] = new SelectList(new List<EmployeeBkp>());
                }
            }

            return departmentEmployees;
        }

        private async Task<IEnumerable<EmployeeBkp>> GetAppropriateEmployeesForStepAsync(Approval step, Requisition requisition)
        {
            if (string.IsNullOrEmpty(step.ApprovalStep))
                return new List<EmployeeBkp>();

            // Handle office employee scenarios
            if (step.ApprovalStep == "Supervisor Approval" ||
                step.ApprovalStep == "Admin Dispatch Approval" ||
                step.ApprovalStep == "HO Employee Receipt")
            {
                // Try to get from department ID first
                var departmentEmployees = await _employeeService.GetEmployeesByDepartmentAsync(step.DepartmentId);

                // If we have a delivery station and no department employees, try by department name
                if ((departmentEmployees == null || !departmentEmployees.Any()) &&
                    !string.IsNullOrEmpty(requisition.DeliveryStation))
                {
                    return await _employeeService.GetEmployeesByDepartmentNameAsync(requisition.DeliveryStation);
                }

                return departmentEmployees ?? new List<EmployeeBkp>();
            }
            // Handle factory employee receipt
            else if (step.ApprovalStep == "Factory Employee Receipt" &&
                     !string.IsNullOrEmpty(requisition.DeliveryStation))
            {
                return await _employeeService.GetFactoryEmployeesByStationAsync(requisition.DeliveryStation);
            }

            // Return empty list for other cases
            return new List<EmployeeBkp>();
        }

    }
}
