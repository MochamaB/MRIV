﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MRIV.Attributes;
using MRIV.Extensions;
using MRIV.Models;
using MRIV.Services;
using MRIV.ViewModels;
using System.Diagnostics;
using System.Net.Sockets;

namespace MRIV.Controllers
{
    [CustomAuthorize]
    public class MaterialRequisitionController : Controller
    {
        private const string WizardViewPath = "~/Views/Wizard/NumberWizard.cshtml";
        private readonly string _connectionString = "Data Source=.;Initial Catalog=Lansupport_5_4;Persist Security Info=True;User ID=sa;Password=P@ssw0rd;Trust Server Certificate=True";
        private readonly IEmployeeService _employeeService;
        private readonly VendorService _vendorService;
        public MaterialRequisitionController(IEmployeeService employeeService, VendorService vendorService)
        {
            _employeeService = employeeService;
            _vendorService = vendorService;
        }


        private List<string> GetSteps()
        {
            return new List<string> { "Ticket", "Requisition Details", "Requisition Items", "Approvers & Recievers", "Summary" };
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

        [HttpGet]
        public async Task<IActionResult> TicketAsync(string search = "")
        { // Query tickets from the database
            var tickets = new List<Ticket>();
            var connectionString = "Data Source=.;Initial Catalog=Lansupport_5_4;Persist Security Info=True;User ID=sa;Password=P@ssw0rd;Trust Server Certificate=True";
            var payrollNo = HttpContext.Session.GetString("EmployeePayrollNo");
            // Get employee and department info
            var (employee, department, station) = await _employeeService.GetEmployeeAndDepartmentAsync(payrollNo);

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = string.IsNullOrEmpty(search)
                    ? @"SELECT TOP 15 [RequestID], [Title], [Description] 
                FROM [Lansupport_5_4].[dbo].[Requests] 
                ORDER BY RequestID DESC"
                    : @"SELECT [RequestID], [Title], [Description] 
                FROM [Lansupport_5_4].[dbo].[Requests] 
                WHERE [Title] LIKE @search OR [Description] LIKE @search 
                ORDER BY RequestID DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(search))
                    {
                        cmd.Parameters.AddWithValue("@search", $"%{search}%");
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

            // Prepare wizard steps and view model
            var steps = GetWizardSteps(currentStep: 1); // Pass the current step
            var viewModel = new MaterialRequisitionWizardViewModel
            {
                Steps = steps,
                CurrentStep = 1,
                PartialBasePath = "~/Views/Shared/CreateRequisition/",
                Tickets = tickets // Add tickets to view model
            };
            ViewBag.Search = search; // Pass the search term
            ViewBag.Steps = GetSteps();
            ViewBag.CurrentStep = "Ticket";
            ViewBag.CurrentStepIndex = 0;

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
            return RedirectToAction("Requisition");
        }

        public async Task<IActionResult> RequisitionAsync()
        {
            // Retrieve the requisition object from the session
            var requisition = HttpContext.Session.GetObject<Requisition>("WizardRequisition");
            var payrollNo = HttpContext.Session.GetString("EmployeePayrollNo");

            // Getlogged in user their employee and department info
            var (employee, department, station) = await _employeeService.GetEmployeeAndDepartmentAsync(payrollNo);
            System.Diagnostics.Debug.WriteLine($"Station: {station?.StationName ?? "null"}");
            using var ktdaContext = new KtdaleaveContext();

            //Get List of Admin Employees for Dispatch
            List<EmployeeBkp> employees = new List<EmployeeBkp>(); // Initialize employees to an empty list
            int AdminDepartmentCode = 104;
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
                Employee = employee,
                EmployeeBkps = employees,
                Department = department,
                Station = station,
                Departments = ktdaContext.Departments.ToList(), // Fetch all departments
                Stations = ktdaContext.Stations.ToList(), // Fetch all stations
                Vendors = await _vendorService.GetVendorsAsync(),

                // Tickets = tickets // Add tickets to view model
            };

         
           

            return View(WizardViewPath, viewModel);
        }

       
       

    }
}
