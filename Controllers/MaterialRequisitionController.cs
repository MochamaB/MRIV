using Microsoft.AspNetCore.Http;
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
        private readonly IStationCategoryService _stationCategoryService;
        public MaterialRequisitionController(IEmployeeService employeeService, VendorService vendorService, RequisitionContext context, 
            IApprovalService approvalService, IConfiguration configuration, IDepartmentService departmentService, IStationCategoryService stationCategoryService)
        {
            _employeeService = employeeService;
            _vendorService = vendorService;
            _context = context;
            _approvalService = approvalService;
            _connectionString = configuration.GetConnectionString("RequisitionContext");
            _departmentService = departmentService;
            _stationCategoryService = stationCategoryService;
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

        private async Task<MaterialRequisitionWizardViewModel> GetWizardViewModelAsync(int currentStep, 
            Requisition requisition = null,
        EmployeeBkp logge = null,  // New optional parameter
        Department preFetchedDepartment = null,
        Station preFetchedStation = null)
        {
            var payrollNo = HttpContext.Session.GetString("EmployeePayrollNo");
            var (loggedInUserEmployee, loggedInUserDepartment, loggedInUserStation) =
                await _employeeService.GetEmployeeAndDepartmentAsync(payrollNo);
            var steps = GetWizardSteps(currentStep);

            using var ktdaContext = new KtdaleaveContext();

            // Get Admin Employees for Dispatch
            int adminDepartmentCode = 106;
            string adminDeptCodeString = adminDepartmentCode.ToString();
            var employees = await ktdaContext.EmployeeBkps
                .Where(e => e.Department == adminDeptCodeString && e.EmpisCurrActive == 0)
                .OrderBy(e => e.Fullname)
                .ToListAsync();
            List<MaterialCategory> materialCategories = await _context.MaterialCategories
                 .ToListAsync();

            return new MaterialRequisitionWizardViewModel
            {
                Steps = steps,
                CurrentStep = currentStep,
                PartialBasePath = "~/Views/Shared/CreateRequisition/",
                Requisition = requisition ?? HttpContext.Session.GetObject<Requisition>("WizardRequisition"),
                LoggedInUserEmployee = loggedInUserEmployee,
                EmployeeBkps = employees,
                LoggedInUserDepartment = loggedInUserDepartment,
                LoggedInUserStation = loggedInUserStation,
                Departments = await ktdaContext.Departments.ToListAsync(),
                Stations = await ktdaContext.Stations.ToListAsync(),
                Vendors = await _vendorService.GetVendorsAsync(),
                MaterialCategories = materialCategories,
            };
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
        
            if (requisition != null)
            {
                // Use the requisition data as needed
                int TicketId = requisition.TicketId;
                string remarks = requisition.Remarks;

                // Pass the data to the view or use it in your logic
                ViewBag.TicketId = TicketId;
                ViewBag.Remark = remarks;
            }

            var viewModel = await GetWizardViewModelAsync(currentStep: 2, requisition);
            // Initialize user location (only if not already set)
            await _stationCategoryService.InitializeUserLocationAsync(
                requisition,
                viewModel.LoggedInUserEmployee,
                viewModel.LoggedInUserDepartment,
                viewModel.LoggedInUserStation
            );
            // Update session with any changes
            HttpContext.Session.SetObject("WizardRequisition", requisition);
            var modelJson = JsonSerializer.Serialize(requisition, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine($"RequisitionModel JSON:\n{modelJson}");
            // Make sure the viewModel has the updated requisition
            viewModel.Requisition = requisition;
            // Populate station categories
            viewModel.IssueStationCategories = await _stationCategoryService.GetStationCategoriesSelectListAsync("issue");
            viewModel.DeliveryStationCategories = await _stationCategoryService.GetStationCategoriesSelectListAsync("delivery");

            // Helper function to load locations
            async Task<SelectList> LoadLocations(string categoryCode, string stationValue)
            {
                return !string.IsNullOrEmpty(categoryCode)
                    ? await _stationCategoryService.GetLocationsForCategoryAsync(categoryCode, stationValue)
                    : new SelectList(new List<SelectListItem>());
            }

            // Load locations for both issue and delivery
            viewModel.IssueLocations = await LoadLocations(requisition.IssueStationCategory, requisition.IssueStation);
            viewModel.DeliveryLocations = await LoadLocations(requisition.DeliveryStationCategory, requisition.DeliveryStation);

            return View(WizardViewPath, viewModel);
        }
        [HttpGet]
        public async Task<IActionResult> GetLocationsForCategory(string category, string selectedValue = null)
        {
            if (string.IsNullOrEmpty(category))
            {
                return Json(new List<object>());
            }

            // Use a different approach - return formatted data directly
            var result = await _stationCategoryService.GetLocationItemsForJsonAsync(category, selectedValue);
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRequisitionAsync(MaterialRequisitionWizardViewModel model, string? direction = null)
        {
            Console.WriteLine($"🔹 Received direction: {direction}");
            // Handle Previous button
            if (direction?.ToLower() == "previous")
            {
                // Navigate to previous step
                return RedirectToAction("Ticket"); // The appropriate previous action
            }
            var requisition = HttpContext.Session.GetObject<Requisition>("WizardRequisition") ?? new Requisition();
            var viewModel = await GetWizardViewModelAsync(currentStep: 2, requisition);
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
                // Set ForwardToAdmin to true if DispatchType is "admin"
                requisition.ForwardToAdmin = model.Requisition.DispatchType == "admin";
                // Set IsExternal to true if DispatchType is "vendor"
                requisition.IsExternal = model.Requisition.DispatchType == "vendor";
            
                // Set CreatedAt to the current date and time
                requisition.CreatedAt = DateTime.Now;
            }
            // Always check model state first
            if (!ModelState.IsValid)
            {
                // Handle validation errors
                viewModel.Requisition = model.Requisition; // Preserve user input
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
                return View(WizardViewPath, viewModel);
             
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
            var requisitionJson = JsonSerializer.Serialize(requisition, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine("Requisition Object:\n" + requisitionJson);
            // ✅ Retrieve logged-in user from the viewModel
            var loggedInUserEmployee =viewModel.LoggedInUserEmployee;
                    // Add Approval steps
                    var approvalSteps = await _approvalService.CreateApprovalStepsAsync(requisition, loggedInUserEmployee);
            // Print to console
            // Print detailed information about the steps
           
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
            // Prepare the wizard view model using the centralized method
            var viewModel = await GetWizardViewModelAsync(currentStep: 3);
            if (viewModel.RequisitionItems == null || !viewModel.RequisitionItems.Any())
            {
                viewModel.RequisitionItems = new List<RequisitionItem>
        {
            new RequisitionItem
            {
                Material = new Material(),
                Status = RequisitionItemStatus.PendingApproval,
                Condition = RequisitionItemCondition.GoodCondition
            }
        };
            }


            return View(WizardViewPath, viewModel);

        }

        public async Task<IActionResult> CreateRequisitionItemsAsync(MaterialRequisitionWizardViewModel model, string? direction = null)
        {
           
            var requisition = HttpContext.Session.GetObject<Requisition>("WizardRequisition");
           
            // 2. Preserve the posted RequisitionItems (cloned rows data)
            var requisitionItems = model.RequisitionItems ?? new List<RequisitionItem>();
            // 3. Ensure Material objects are initialized
            foreach (var item in requisitionItems)
            {
                // Only create/update material if SaveToInventory is true or Material.Code exists
                if (item.SaveToInventory && !string.IsNullOrEmpty(item.Material?.Code))
                {
                    item.Material ??= new Material();
                    item.Material.Name = item.Name;
                    item.Material.Description = item.Description;
                    item.Material.CurrentLocationId = requisition.IssueStation;

                    // Add to materials list for saving

                }
                else
                {
                    // Clear material association
                    item.Material = null;
                    item.MaterialId = null;
                }

            }

            if (!ModelState.IsValid)
            {
                var viewModel = await GetWizardViewModelAsync(currentStep: 3);
                // 5. Repopulate other critical view data
                viewModel.RequisitionItems = requisitionItems;
                // Initialize Material for each RequisitionItem if it's null
                foreach (var item in viewModel.RequisitionItems)
                {
                    item.Material ??= new Material();
                }
                var modelJson = JsonSerializer.Serialize(model.RequisitionItems, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine($"Requisition Items Object JSON:\n{modelJson}");

                // 🚀 Log all validation errors for debugging
                var errors = ModelState.Where(m => m.Value.Errors.Any())
                                       .Select(m => new { Field = m.Key, Errors = m.Value.Errors.Select(e => e.ErrorMessage) })
                                       .ToList();

                var errorsJson = JsonSerializer.Serialize(errors, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine($"Validation Errors:\n{errorsJson}");
                TempData["ErrorMessage"] = "Check Validation Errors Below!";

                // Return view directly (NO REDIRECTION)
                return View(WizardViewPath, viewModel);
            }
            HttpContext.Session.SetObject("WizardRequisitionItems", requisitionItems);
            var requisitionItemsJson = JsonSerializer.Serialize(requisitionItems, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine("Requisition Items:\n" + requisitionItemsJson);

            // Set a success message in TempData
            TempData["SuccessMessage"] = "Requisition Items Added successfully!";

            // Redirect to next step
            return RedirectToAction("ApproversReceivers");

        }

        public async Task<IActionResult> ApproversReceiversAsync()
        {
            var requisition = HttpContext.Session.GetObject<Requisition>("WizardRequisition");
            var approvalSteps = HttpContext.Session.GetObject<List<Approval>>("WizardApprovalSteps");

            if (requisition == null || approvalSteps == null)
            {
                // Handle the case where the approval steps are not found in the session
                return RedirectToAction("Requisition");
            }
            // Prepare wizard steps and view model
            var steps = GetWizardSteps(currentStep: 4);
            var viewModel = new MaterialRequisitionWizardViewModel
            {
                Steps = steps,
                CurrentStep = 4,
                PartialBasePath = "~/Views/Shared/CreateRequisition/",
                Requisition = requisition,
                ApprovalSteps = new List<ApprovalStepViewModel>(),
                DepartmentEmployees = new Dictionary<string, SelectList>()
            };

            // Get vendors for vendor steps
            var vendors = await _vendorService.GetVendorsAsync();

            // Use service to convert approval steps to view models
            viewModel.ApprovalSteps = await _approvalService.ConvertToViewModelsAsync(approvalSteps, requisition, vendors);

            // Use service to populate department employees
            viewModel.DepartmentEmployees = await _approvalService.PopulateDepartmentEmployeesAsync(requisition, approvalSteps);

            return View(WizardViewPath, viewModel);
        }

        [HttpPost]
    public async Task<IActionResult> CreateApprovals(IFormCollection form)
    {
            // 1. Retrieve session data
            var requisition = HttpContext.Session.GetObject<Requisition>("WizardRequisition");
            var approvalSteps = HttpContext.Session.GetObject<List<Approval>>("WizardApprovalSteps");

            if (requisition == null || approvalSteps == null)
            {
                return RedirectToAction("Index"); // Handle missing session data
            }

            // 2. Update collector information
            requisition.CollectorName = form["Requisition.CollectorName"];
            requisition.CollectorId = form["Requisition.CollectorId"];

            // 3. Update selected approvers
            foreach (var step in approvalSteps)
            {
                var key = $"SelectedEmployee_{step.StepNumber}";
                if (form.ContainsKey(key))
                {
                    var selectedPayroll = form[key];
                    if (!string.IsNullOrEmpty(selectedPayroll))
                    {
                        // Update approval step with selected employee
                        step.PayrollNo = selectedPayroll;
                        var employee = await _employeeService.GetEmployeeByPayrollAsync(selectedPayroll);
                        // If this is a regular employee (not vendor dispatch)
                        if (step.ApprovalStep != "Vendor Dispatch" && employee != null)
                        {
                            // Update the department ID based on the selected employee
                            step.DepartmentId = Convert.ToInt32(employee.Department);

                            // If this is HO Employee Receipt or Factory Employee Receipt,
                            // we might need to update other related information as well
                            if (step.ApprovalStep == "Factory Employee Receipt" ||
                                step.ApprovalStep == "HO Employee Receipt")
                            {
                                // You might want to update requisition.DeliveryStation here
                                // if employee selection should affect that
                            }
                        }

                    }
                }
            }

                // 4. Save updated data to session
                HttpContext.Session.SetObject("WizardRequisition", requisition);
                HttpContext.Session.SetObject("WizardApprovalSteps", approvalSteps);

                    // Set a success message in TempData
                    TempData["SuccessMessage"] = "Approvals and Receivers Added successfully!";

                    // 5. Move to next wizard step (assuming current step is 4)
                    return RedirectToAction("WizardSummary");
    }

        [HttpGet]
        public async Task<IActionResult> WizardSummaryAsync()
        {
            // Retrieve the requisition object from the session
            var requisition = HttpContext.Session.GetObject<Requisition>("WizardRequisition");
            var requisitionItems = HttpContext.Session.GetObject<List<RequisitionItem>>("WizardRequisitionItems");
            var approvalSteps = HttpContext.Session.GetObject<List<Approval>>("WizardApprovalSteps");
            var materialCategories = await _context.MaterialCategories.ToListAsync();
            var vendors = await _vendorService.GetVendorsAsync();

           
            // Log the session data to the console
            Console.WriteLine("---------- Session Data Debugging ----------");

            // Log Requisition Object
            if (requisition != null)
            {
                var requisitionJson = JsonSerializer.Serialize(requisition, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine("Requisition Object:\n" + requisitionJson);
            }
            else
            {
                Console.WriteLine("Requisition object is null.");
            }

            // Log Requisition Items
            if (requisitionItems != null && requisitionItems.Any())
            {
                var requisitionItemsJson = JsonSerializer.Serialize(requisitionItems, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine("Requisition Items:\n" + requisitionItemsJson);
            }
            else
            {
                Console.WriteLine("Requisition items are null or empty.");
            }

            // Log Approval Steps
            if (approvalSteps != null && approvalSteps.Any())
            {
                var approvalStepsJson = JsonSerializer.Serialize(approvalSteps, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine("Approval Steps:\n" + approvalStepsJson);
            }
            else
            {
                Console.WriteLine("Approval steps are null or empty.");
            }


            Console.WriteLine("--------------------------------------------");


          
            // Use the service to convert approval steps
            var approvalStepViewModels = await _approvalService.ConvertToViewModelsAsync(
                approvalSteps ?? new List<Approval>(),
                requisition ?? new Requisition(),
                vendors ?? new List<Vendor>()
            );


            // Prepare wizard steps and view model
            var steps = GetWizardSteps(currentStep: 5); // Pass the current step
            var viewModel = new MaterialRequisitionWizardViewModel
            {
                Steps = steps,
                CurrentStep = 3,
                PartialBasePath = "~/Views/Shared/CreateRequisition/",
                Requisition = requisition,
                RequisitionItems = requisitionItems,
                ApprovalSteps = approvalStepViewModels,
                employeeDetail = await _employeeService.GetEmployeeByPayrollAsync(requisition.PayrollNo),
                departmentDetail = await _departmentService.GetDepartmentByIdAsync(requisition.DepartmentId),
                dispatchEmployee = await _employeeService.GetEmployeeByPayrollAsync(requisition.DispatchPayrollNo),
                vendor = await _vendorService.GetVendorByIdAsync(requisition.DispatchVendor),
                Vendors = vendors
               
            };

            return View(WizardViewPath, viewModel);

        }
        [HttpPost]
        public async Task<IActionResult> CompleteWizard()
        {
            // Retrieve the requisition object from the session
            var requisition = HttpContext.Session.GetObject<Requisition>("WizardRequisition");
            var requisitionItems = HttpContext.Session.GetObject<List<RequisitionItem>>("WizardRequisitionItems");
            var approvalSteps = HttpContext.Session.GetObject<List<Approval>>("WizardApprovalSteps");
            if (requisition == null || approvalSteps == null || approvalSteps ==null)
            {
                return RedirectToAction("Ticket"); // Handle missing session data
            }
            // Save the requisition
            requisition.Status = RequisitionStatus.PendingApproval;
            _context.Requisitions.Add(requisition);
            await _context.SaveChangesAsync();

            // Redirect to the Requisition Index action
            TempData["SuccessMessage"] = "Material Requisition has been created successfully.Go to approvals to track progress";
            return RedirectToAction("Index", "Requisitions");
        }

        }
    }
