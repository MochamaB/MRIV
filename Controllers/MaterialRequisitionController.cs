using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Mono.TextTemplating;
using MRIV.Attributes;
using MRIV.Enums;
using MRIV.Extensions;
using MRIV.Models;
using MRIV.Services;
using MRIV.ViewModels;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;
using static System.Collections.Specialized.BitVector32;

namespace MRIV.Controllers
{
    [CustomAuthorize]
    public class MaterialRequisitionController : Controller
    {
        private const string WizardViewPath = "~/Views/Wizard/NumberWizard.cshtml";
      //  private readonly string _connectionString = "Data Source=.;Initial Catalog=Lansupport_5_4;Persist Security Info=True;User ID=sa;Password=P@ssw0rd;Trust Server Certificate=True";
        private readonly IEmployeeService _employeeService;
        private readonly VendorService _vendorService;
        private readonly IDepartmentService _departmentService;
        private readonly RequisitionContext _context;
        private readonly IApprovalService _approvalService;
        private readonly string _connectionString;
        public MaterialRequisitionController(IEmployeeService employeeService, VendorService vendorService, RequisitionContext context, 
            IApprovalService approvalService, IConfiguration configuration, IDepartmentService departmentService)
        {
            _employeeService = employeeService;
            _vendorService = vendorService;
            _context = context;
            _approvalService = approvalService;
            _connectionString = configuration.GetConnectionString("RequisitionContext");
            _departmentService = departmentService;
        }


        private List<string> GetSteps()
        {
            return new List<string> { "Ticket", "Requisition Details", "Requisition Items", "Approvers & Receivers", "Summary" };
        }
        private List<WizardStepViewModel> GetWizardSteps(int currentStep)
        {
            return GetSteps().Select((step, index) => new WizardStepViewModel
            {
                StepName = step,
                StepNumber = index + 1,
                IsActive = index + 1 == currentStep,
                IsCompleted = index + 1 < currentStep
            }).ToList();
        }

        private async Task<MaterialRequisitionWizardViewModel> InitializeWizardModelAsync(MaterialRequisitionWizardViewModel model,HttpContext httpContext,int currentStep)
        {
            var payrollNo = httpContext.Session.GetString("EmployeePayrollNo");
            var (loggedInUserEmployee, loggedInUserDepartment, loggedInUserStation) = await _employeeService.GetEmployeeAndDepartmentAsync(payrollNo);

            // Initialize null properties
            model.LoggedInUserEmployee = loggedInUserEmployee ?? new EmployeeBkp();
            model.LoggedInUserDepartment = loggedInUserDepartment ?? new Department();
            model.LoggedInUserStation = loggedInUserStation ?? new Station();
            model.Requisition ??= new Requisition();
            // ✅ Preserve existing RequisitionItems (DO NOT overwrite)
            var existingRequisitionItems = model.RequisitionItems ?? new List<RequisitionItem>();

            using var ktdaContext = new KtdaleaveContext();

            // Get Admin Employees for Dispatch
            model.EmployeeBkps = await ktdaContext.EmployeeBkps
                .Where(e => e.Department == "106" && e.EmpisCurrActive == 0)
                .OrderBy(e => e.Fullname)
                .ToListAsync();

            // Populate Departments and Stations
            model.Departments = await ktdaContext.Departments.ToListAsync();
            model.Stations = await ktdaContext.Stations.ToListAsync();

            // Populate Vendors
            model.Vendors = await _vendorService.GetVendorsAsync();
            // ✅ Restore `RequisitionItems`
            model.RequisitionItems = existingRequisitionItems;

            // Set common paths
            model.PartialBasePath = "~/Views/Shared/CreateRequisition/";

            return model;
        }

        //1. SELECT TICKET ////////////////////


        [HttpGet]
        public async Task<IActionResult> TicketAsync(string search = "")
        {
            var tickets = new List<Ticket>();
         //   var connectionString = "Data Source=.;Initial Catalog=Lansupport_5_4;Persist Security Info=True;User ID=sa;Password=P@ssw0rd;Trust Server Certificate=True";
            var payrollNo = HttpContext.Session.GetString("EmployeePayrollNo");
            var (loggedInUserEmployee, loggedInUserDepartment, loggedInUserStation) = await _employeeService.GetEmployeeAndDepartmentAsync(payrollNo);

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query;
                bool isNumeric = false;

                if (string.IsNullOrEmpty(search))
                {
                    query = @"SELECT TOP 15 [RequestID], [Title], [Description] 
                      FROM [Lansupport_5_4].[dbo].[Requests] 
                      ORDER BY RequestID DESC";
                }
                else
                {
                    isNumeric = int.TryParse(search, out int _);
                    query = isNumeric
                        ? @"SELECT [RequestID], [Title], [Description] 
                   FROM [Lansupport_5_4].[dbo].[Requests] 
                   WHERE CAST([RequestID] AS VARCHAR) LIKE @search + '%' 
                   ORDER BY RequestID DESC"
                        : @"SELECT [RequestID], [Title], [Description] 
                   FROM [Lansupport_5_4].[dbo].[Requests] 
                   WHERE [Title] LIKE @search OR [Description] LIKE @search 
                   ORDER BY RequestID DESC";
                }

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(search))
                    {
                        cmd.Parameters.AddWithValue("@search", isNumeric ? search : $"%{search}%");
                    }

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tickets.Add(new Ticket
                            {
                                RequestID = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Description = reader.GetString(2)
                            });
                        }
                    }
                }
            }

            var steps = GetWizardSteps(currentStep: 1);
            var viewModel = new MaterialRequisitionWizardViewModel
            {
                Steps = steps,
                CurrentStep = 1,
                PartialBasePath = "~/Views/Shared/CreateRequisition/",
                Tickets = tickets
            };

            ViewBag.Search = search;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("CreateRequisition/_TicketTable", viewModel);
            }
            return View(WizardViewPath, viewModel);
        }

        private List<Ticket> QueryTicketsFromDatabase(string search = "")
        {
            var tickets = new List<Ticket>();
            var connectionString = "Data Source=.;Initial Catalog=Lansupport_5_4;Persist Security Info=True;User ID=sa;Password=P@ssw0rd;Trust Server Certificate=True";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = string.IsNullOrEmpty(search)
                    ? @"SELECT TOP 15 [RequestID], [Title], [Description] 
               FROM [Lansupport_5_4].[dbo].[Requests] 
               ORDER BY RequestID DESC"
                    : @"SELECT [RequestID], [Title], [Description] 
               FROM [Lansupport_5_4].[dbo].[Requests] 
               WHERE [RequestID] = @search 
               ORDER BY RequestID DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(search))
                    {
                        // Ensure the search term is parsed as an integer for RequestID
                        if (int.TryParse(search, out int requestId))
                        {
                            cmd.Parameters.AddWithValue("@search", requestId);
                        }
                        else
                        {
                            // If the search term is not a valid integer, return an empty list
                            return tickets;
                        }
                    }

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tickets.Add(new Ticket
                            {
                                RequestID = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Description = reader.GetString(2)
                            });
                        }
                    }
                }
            }

            return tickets;
        }

        public async Task<IActionResult> SelectTicket(int RequisitionID, string Description)
        {
            // Create a requisition object (or use your existing model)
            var requisition = new Requisition
            {
                TicketId = RequisitionID,
                Remarks = Description
            };

            // Store the requisition object in the session
            HttpContext.Session.SetObject("WizardRequisition", requisition);

            // Set a success message in TempData
            TempData["SuccessMessage"] = "Ticket selected successfully!";

            // Redirect to the Requisition action
            return RedirectToAction("RequisitionDetails");
        }

    //2. ENTER AND SAVE THE REQUISITION DETAILS ////////////////////

        public async Task<IActionResult> RequisitionDetailsAsync()
        {
            // Retrieve the requisition object from the session
            var requisition = HttpContext.Session.GetObject<Requisition>("WizardRequisition");
            var payrollNo = HttpContext.Session.GetString("EmployeePayrollNo");

            // Getlogged in user their employee and department info
            var (loggedInUserEmployee, loggedInUserDepartment, loggedInUserStation) = await _employeeService.GetEmployeeAndDepartmentAsync(payrollNo);
            System.Diagnostics.Debug.WriteLine($"Station: {loggedInUserStation?.StationName ?? "null"}");
            using var ktdaContext = new KtdaleaveContext();

            //Get List of Admin Employees for Dispatch
            List<EmployeeBkp> employees = new List<EmployeeBkp>(); // Initialize employees to an empty list
            int AdminDepartmentCode = 106;
            string AdmindepartmentCodeString = AdminDepartmentCode.ToString();
            employees = await ktdaContext.EmployeeBkps.Where(e => e.Department == AdmindepartmentCodeString &&
                   e.EmpisCurrActive == 0).OrderBy(e => e.Fullname).ToListAsync();

            if (requisition != null)
            {
                // Use the requisition data as needed
                int TicketId = requisition.TicketId;
                string remarks = requisition.Remarks;

                // Pass the data to the view or use it in your logic
                ViewBag.TicketId = TicketId;
                ViewBag.Remark = remarks;
            }

            // Prepare wizard steps and view model
            var steps = GetWizardSteps(currentStep: 2); // Pass the current step
            var viewModel = new MaterialRequisitionWizardViewModel
            {
                Steps = steps,
                CurrentStep = 2,
                PartialBasePath = "~/Views/Shared/CreateRequisition/",
                Requisition = requisition,
                LoggedInUserEmployee = loggedInUserEmployee,
                EmployeeBkps = employees,
                LoggedInUserDepartment = loggedInUserDepartment,
                LoggedInUserStation = loggedInUserStation,
                Departments = ktdaContext.Departments.ToList(), // Fetch all departments
                Stations = ktdaContext.Stations.ToList(), // Fetch all stations
                Vendors = await _vendorService.GetVendorsAsync(),

                // Tickets = tickets // Add tickets to view model
            };

         
           

            return View(WizardViewPath, viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRequisitionAsync(MaterialRequisitionWizardViewModel model, string? direction = null)
        {
            Console.WriteLine($"🔹 Received direction: {direction}");
            var requisition = HttpContext.Session.GetObject<Requisition>("WizardRequisition") ?? new Requisition();
            var steps = GetWizardSteps(currentStep: 2);
            var payrollNo = HttpContext.Session.GetString("EmployeePayrollNo");

            // Get logged-in user details
            var(loggedInUserEmployee, loggedInUserDepartment, loggedInUserStation) = await _employeeService.GetEmployeeAndDepartmentAsync(payrollNo);

            if (model.Requisition != null)
            {

                // Map properties
                requisition.DepartmentId = model.Requisition.DepartmentId;
                requisition.PayrollNo = model.Requisition.PayrollNo;
                requisition.IssueStationCategory = model.Requisition.IssueStationCategory;
                requisition.IssueStation = model.Requisition.IssueStation;
                requisition.DeliveryStationCategory = model.Requisition.DeliveryStationCategory;
                requisition.DeliveryStation = model.Requisition.DeliveryStation;

                // Conditional mapping
                requisition.DispatchType = model.Requisition.DispatchType;
                requisition.DispatchPayrollNo = model.Requisition.DispatchType == "admin"
                    ? model.Requisition.DispatchPayrollNo
                    : null;

                requisition.DispatchVendor = model.Requisition.DispatchType == "vendor"
                    ? model.Requisition.DispatchVendor
                : null;
            }
            // Always check model state first
            if (!ModelState.IsValid)
            {
                // Handle validation errors
                // Instead of redirecting, fetch needed data and return the same view
                

                using var ktdaContext = new KtdaleaveContext();

                // Get Admin Employees for Dispatch
                int AdminDepartmentCode = 106;
                string AdmindepartmentCodeString = AdminDepartmentCode.ToString();
                List<EmployeeBkp> employees = await ktdaContext.EmployeeBkps
                    .Where(e => e.Department == AdmindepartmentCodeString && e.EmpisCurrActive == 0)
                    .OrderBy(e => e.Fullname)
                    .ToListAsync();

                // 2. Initialize the model (this resets RequisitionItems to default)
                model = await InitializeWizardModelAsync(model, HttpContext, currentStep: 3);


                // Populate the model again to pass back to the view
                model.Steps = steps;
                model.CurrentStep = 2;
                model.Requisition = requisition;
                model.EmployeeBkps = employees;
                model.Departments = ktdaContext.Departments.ToList();
                model.Stations = ktdaContext.Stations.ToList();
                model.Vendors = await _vendorService.GetVendorsAsync();
                var modelJson = JsonSerializer.Serialize(model.Requisition, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine($"MaterialRequisitionModel JSON:\n{modelJson}");

                // 🚀 Log all validation errors for debugging
                var errors = ModelState.Where(m => m.Value.Errors.Any())
                                       .Select(m => new { Field = m.Key, Errors = m.Value.Errors.Select(e => e.ErrorMessage) })
                                       .ToList();

                var errorsJson = JsonSerializer.Serialize(errors, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine($"Validation Errors:\n{errorsJson}");
                TempData["ErrorMessage"] = "Check Validation Errors Below!";

                // Return view directly (NO REDIRECTION)
                return View(WizardViewPath, model);
             
            }


            

            // 🚀 Handle Previous Step Logic
            // 🚀 Handle Previous Step Logic
            if (!string.IsNullOrEmpty(direction) && direction.ToLower() == "previous")
            {
                model.CurrentStep = Math.Max(1, model.CurrentStep - 1);
                model.PartialBasePath = "~/Views/Shared/CreateRequisition/";
                Console.WriteLine("⬅ Moving to Previous Step");

                // Save progress in session
                HttpContext.Session.SetObject("WizardRequisition", requisition);

                // 🚀 Return the same view instead of redirecting
                return View(WizardViewPath, model);
            }
            // Save updated requisition back to session
            HttpContext.Session.SetObject("WizardRequisition", requisition);

            // Add Approval steps
            var approvalSteps = await _approvalService.CreateApprovalStepsAsync(requisition, loggedInUserEmployee);
            // Print to console
            // Print detailed information about the steps
            Console.WriteLine($"Number of approval steps created: {approvalSteps?.Count ?? 0}");
            foreach (var step in approvalSteps ?? new List<Approval>())
            {
                        Console.WriteLine($"""
                Step: {step.ApprovalStep}
                Status: {step.ApprovalStatus}
                PayrollNo: {step.PayrollNo}
                RequisitionId: {step.RequisitionId}
                DepartmentId: {step.DepartmentId}
                CreatedAt: {step.CreatedAt}
                """);
            }
            Console.WriteLine("Approval Steps:");
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(approvalSteps, Newtonsoft.Json.Formatting.Indented));
            HttpContext.Session.SetObject("WizardApprovalSteps", approvalSteps);


            // Set a success message in TempData
            TempData["SuccessMessage"] = "Requisition Added successfully!";

            // Redirect to next step
            return RedirectToAction("RequisitionItems");

        }

        public async Task<IActionResult> RequisitionItemsAsync()
        {
            // Retrieve the requisition object from the session
            var requisition = HttpContext.Session.GetObject<Requisition>("WizardRequisition");
            // Prepare wizard steps and view model
            var steps = GetWizardSteps(currentStep: 3); // Pass the current step
            List<MaterialCategory> materialCategories = await _context.MaterialCategories
                   .ToListAsync();
            var viewModel = new MaterialRequisitionWizardViewModel
            {
                Steps = steps,
                CurrentStep = 3,
                PartialBasePath = "~/Views/Shared/CreateRequisition/",
                Requisition = requisition,
                RequisitionItems = new List<RequisitionItem>
                {
                    new RequisitionItem
                    {
                        Material = new Material(),
                        Status = RequisitionItemStatus.PendingApproval,
                        Condition = RequisitionItemCondition.GoodCondition
                    }
                },
                MaterialCategories = materialCategories,
                Vendors = await _vendorService.GetVendorsAsync()


        };

            return View(WizardViewPath, viewModel);

        }

        public async Task<IActionResult> CreateRequisitionItemsAsync(MaterialRequisitionWizardViewModel model, string? direction = null)
        {
           
            var steps = GetWizardSteps(currentStep: 3);
            List<MaterialCategory> materialCategories = await _context.MaterialCategories
                   .ToListAsync();
            var vendors = await _vendorService.GetVendorsAsync();
            // 1. Retrieve the requisition object from the session
            var requisition = HttpContext.Session.GetObject<Requisition>("WizardRequisition");
            // 2. Preserve the posted RequisitionItems (cloned rows data)
            var requisitionItems = model.RequisitionItems ?? new List<RequisitionItem>();
            // 3. Ensure Material objects are initialized
            foreach (var item in requisitionItems)
            {
                item.Material ??= new Material();
                item.Material.Name = item.Name;
                item.Material.Description = item.Description;
                item.Material.CurrentLocationId = requisition.IssueStation;
            //    item.Material.Status = goodCondition;
            }

            if (!ModelState.IsValid)
            {
               
                // 2. Initialize the model (this resets RequisitionItems to default)
                model = await InitializeWizardModelAsync(model, HttpContext, currentStep: 3);

                // 5. Repopulate other critical view data
                model.RequisitionItems = requisitionItems;
                model.MaterialCategories = await _context.MaterialCategories.ToListAsync();
                model.Vendors = await _vendorService.GetVendorsAsync();
                model.Steps = steps;
                model.CurrentStep = 3;


                var modelJson = JsonSerializer.Serialize(model.RequisitionItems, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine($"MaterialRequisitionModel JSON:\n{modelJson}");

                // 🚀 Log all validation errors for debugging
                var errors = ModelState.Where(m => m.Value.Errors.Any())
                                       .Select(m => new { Field = m.Key, Errors = m.Value.Errors.Select(e => e.ErrorMessage) })
                                       .ToList();

                var errorsJson = JsonSerializer.Serialize(errors, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine($"Validation Errors:\n{errorsJson}");
                TempData["ErrorMessage"] = "Check Validation Errors Below!";

                // Return view directly (NO REDIRECTION)
                return View(WizardViewPath, model);
            }
            HttpContext.Session.SetObject("WizardRequisitionItems", requisitionItems);


            // Set a success message in TempData
            TempData["SuccessMessage"] = "Requisition Items Added successfully!";

            // Redirect to next step
            return RedirectToAction("ApproversReceivers");

        }

        public async Task<IActionResult> ApproversReceiversAsync()
        {
            // Retrieve the requisition object from the session
            var requisition = HttpContext.Session.GetObject<Requisition>("WizardRequisition");

            // Retrieve the approval steps from the session
            var approvalSteps = HttpContext.Session.GetObject<List<Approval>>("WizardApprovalSteps");
            // Prepare wizard steps and view model
            var steps = GetWizardSteps(currentStep: 4); // Pass the current step
            var jsonString = System.Text.Json.JsonSerializer.Serialize(approvalSteps, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            Console.WriteLine(jsonString);

            var viewModel = new MaterialRequisitionWizardViewModel
            {
                Steps = steps,
                CurrentStep = 4,
                PartialBasePath = "~/Views/Shared/CreateRequisition/",
                Requisition = requisition,
                ApprovalSteps = new List<ApprovalStepViewModel>(),
                DepartmentEmployees = new Dictionary<string, SelectList>()

            };
            // Populate view model with employee/department names
            if (approvalSteps != null)
            {
                for (int i = 0; i < approvalSteps.Count; i++)
                {
                    var step = approvalSteps[i];
                    var department = await _departmentService.GetDepartmentByIdAsync(step.DepartmentId);
                    var employee = await _employeeService.GetEmployeeByPayrollAsync(step.PayrollNo);
                    string departmentName = department?.DepartmentName ?? "Unknown";
                    string employeeName = employee?.Fullname ?? "Unknown";
                    // Handle Vendor Dispatch
                    // Handle Vendor Dispatch
                    if (step.ApprovalStep == "Vendor Dispatch")
                    {
                        if (int.TryParse(step.PayrollNo, out int vendorId))
                        {
                            var vendor = (await _vendorService.GetVendorsAsync()).FirstOrDefault(v => v.VendorID == vendorId);
                            employeeName = vendor?.Name ?? "Unknown Vendor";
                        }
                        departmentName = "N/A"; // Override department for vendors
                    }
                    // Handle Factory Employee Receipt station
                    if (step.ApprovalStep == "Factory Employee Receipt" && employee != null)
                    {
                        departmentName += $" ({employee.Station})";
                    }

                    viewModel.ApprovalSteps.Add(new ApprovalStepViewModel
                    {
                        StepNumber = i + 1,
                        ApprovalStep = step.ApprovalStep,
                        PayrollNo = step.PayrollNo,
                        EmployeeName = employeeName, // Use computed employeeName (vendor name)
                        DepartmentId = step.DepartmentId,
                        DepartmentName = departmentName, // Use computed departmentName ("N/A" for vendors)
                        ApprovalStatus = step.ApprovalStatus,
                        CreatedAt = step.CreatedAt

                    });
                  
                 
                    // Determine which employees to fetch based on step type

                    // Initialize employees as empty list
                    IEnumerable<EmployeeBkp> employees = new List<EmployeeBkp>();

                    // Only fetch employees if the step type matches and required data is available
                    if (!string.IsNullOrEmpty(step.ApprovalStep))
                    {
                        if ((step.ApprovalStep == "Supervisor Approval" ||
                             step.ApprovalStep == "Admin Dispatch Approval" ||
                             step.ApprovalStep == "HO Employee Receipt") &&
                            step.DepartmentId != 0)
                        {
                            var deptEmployees = await _employeeService.GetEmployeesByDepartmentAsync(step.DepartmentId);
                            if (deptEmployees != null)
                            {
                                employees = deptEmployees;
                            }
                        }
                        else if (step.ApprovalStep == "Factory Employee Receipt" && employee?.Station != null)
                        {
                            var stationEmployees = await _employeeService.GetEmployeesByStationAsync(employee.Station);
                            if (stationEmployees != null)
                            {
                                employees = stationEmployees;
                            }
                        }

                        // Create SelectList only if we have valid employees
                        if (employees != null && employees.Any())
                        {
                            viewModel.DepartmentEmployees[step.ApprovalStep] = new SelectList(
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
                            viewModel.DepartmentEmployees[step.ApprovalStep] = new SelectList(new List<EmployeeBkp>());
                        }
                    }
                }
            }

            return View(WizardViewPath, viewModel);
        }




    }
    }
