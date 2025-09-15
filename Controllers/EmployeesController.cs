using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MRIV.Attributes;
using MRIV.Extensions;
using MRIV.Models;
using MRIV.Models.Views;
using MRIV.Services;
using MRIV.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MRIV.Controllers
{
    [CustomAuthorize]
    public class EmployeesController : Controller
    {
        private readonly RequisitionContext _context;
        private readonly KtdaleaveContext _ktdaContext;
        private readonly IEmployeeService _employeeService;
        private readonly IDepartmentService _departmentService;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(
            RequisitionContext context,
            KtdaleaveContext ktdaContext,
            IEmployeeService employeeService,
            IDepartmentService departmentService,
            ILogger<EmployeesController> logger)
        {
            _context = context;
            _ktdaContext = ktdaContext;
            _employeeService = employeeService;
            _departmentService = departmentService;
            _logger = logger;
        }

        // GET: Employees
        public async Task<IActionResult> Index(int page = 1, string sortField = "Fullname", string sortOrder = "asc", string searchTerm = "")
        {
            // Set page size
            int pageSize = 50;

            // Ensure valid pagination parameters
            page = page < 1 ? 1 : page;

            // Start with the optimized view query - single source with all resolved names
            var employeesQuery = _context.EmployeeDetailsViews
                .AsNoTracking()
                .AsQueryable();

            // Get filter values from request query string
            var filters = new Dictionary<string, string>();
            foreach (var key in Request.Query.Keys.Where(k => k != "page" && k != "pageSize" && k != "sortField" && k != "sortOrder" && k != "searchTerm"))
            {
                if (!string.IsNullOrEmpty(Request.Query[key]))
                {
                    filters[key] = Request.Query[key];
                }
            }

            // Get distinct values for filter dropdowns directly from the view
            var allDepartments = await employeesQuery
                .Where(e => !string.IsNullOrEmpty(e.DepartmentName) && !string.IsNullOrEmpty(e.DepartmentCode))
                .Select(e => new { Id = e.DepartmentCode, Name = e.DepartmentName })
                .Distinct()
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            var allStations = await employeesQuery
                .Where(e => !string.IsNullOrEmpty(e.StationName) && !string.IsNullOrEmpty(e.OriginalStationName))
                .Select(e => new { Id = e.OriginalStationName, Name = e.StationName })
                .Distinct()
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            // Create display names dictionary for filters
            var displayNames = new Dictionary<string, Dictionary<string, string>>();
            if (allDepartments.Any())
                displayNames.Add("DepartmentCode", allDepartments);
            if (allStations.Any())
                displayNames.Add("OriginalStationName", allStations);

            // Create filters using the view properties
            ViewBag.Filters = await employeesQuery.CreateFiltersAsync(
                new Expression<Func<EmployeeDetailsView, object>>[] {
                    e => e.OriginalStationName,
                    e => e.DepartmentCode,
                    e => e.Role,
                    e => e.Designation
                },
                filters,
                displayNames
            );

            // Apply filters to query
            employeesQuery = employeesQuery.ApplyFilters(filters);

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
                employeesQuery = employeesQuery.Where(e =>
                    (e.Fullname != null && e.Fullname.ToLower().Contains(searchTerm)) ||
                    (e.PayrollNo != null && e.PayrollNo.ToLower().Contains(searchTerm)) ||
                    (e.Designation != null && e.Designation.ToLower().Contains(searchTerm)) ||
                    (e.EmailAddress != null && e.EmailAddress.ToLower().Contains(searchTerm)) ||
                    (e.DepartmentName != null && e.DepartmentName.ToLower().Contains(searchTerm)) ||
                    (e.StationName != null && e.StationName.ToLower().Contains(searchTerm)));
            }

            // Get total count for pagination after filtering
            var totalEmployees = await employeesQuery.CountAsync();

            // Apply sorting - map old sort fields to view properties
            employeesQuery = ApplyViewSorting(employeesQuery, sortField, sortOrder);

            // Apply pagination
            var employees = await employeesQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Set up pagination view model
            var paginationViewModel = new PaginationViewModel
            {
                CurrentPage = page,
                TotalItems = totalEmployees,
                ItemsPerPage = pageSize,
                Action = "Index",
                Controller = "Employees"
            };

            // Pass data to view - no need for additional dictionary lookups since view has resolved names
            ViewBag.Pagination = paginationViewModel;
            ViewBag.TotalCount = totalEmployees;
            ViewBag.SortField = sortField;
            ViewBag.SortOrder = sortOrder;
            ViewBag.SearchTerm = searchTerm;

            // Create view models directly from the view data (all names are already resolved)
            var viewModels = employees.Select(employee => new EmployeeViewModel
            {
                PayrollNo = employee.PayrollNo,
                RollNo = employee.RollNo,
                FullName = employee.Fullname,
                SurName = employee.SurName,
                OtherNames = employee.OtherNames,
                Department = employee.DepartmentCode,
                DepartmentName = employee.DepartmentName ?? "Unknown",
                Station = employee.OriginalStationName,
                StationName = employee.StationName ?? "Unknown",
                StationCategoryCode = employee.StationCategoryCode,
                StationCategoryName = employee.StationCategoryName ?? "Unknown",
                Role = employee.Role,
                Designation = employee.Designation,
                EmailAddress = employee.EmailAddress,
                SupervisorName = employee.SupervisorName,
                HeadOfDepartmentName = employee.HeadOfDepartmentName,
                Scale = employee.Scale,
                EmpisCurrActive = employee.IsActive
            }).ToList();

            return View(viewModels);
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(string id, string rollNo)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(rollNo))
            {
                return NotFound();
            }

            var employee = await _ktdaContext.EmployeeBkps
                .FirstOrDefaultAsync(m => m.PayrollNo == id && m.RollNo == rollNo);
                
            if (employee == null)
            {
                return NotFound();
            }

            // Get department name
            if (!string.IsNullOrEmpty(employee.Department))
            {
                try
                {
                    int departmentId;
                    if (int.TryParse(employee.Department, out departmentId))
                    {
                        var department = await _departmentService.GetDepartmentByIdAsync(departmentId);
                        ViewBag.DepartmentName = department?.DepartmentName ?? "Unknown";
                    }
                    else
                    {
                        ViewBag.DepartmentName = employee.Department;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting department name for ID: {DepartmentId}", employee.Department);
                    ViewBag.DepartmentName = "Error retrieving name";
                }
            }
            else
            {
                ViewBag.DepartmentName = "Not assigned";
            }

            // Get station name
            if (!string.IsNullOrEmpty(employee.Station))
            {
                try
                {
                    if (employee.Station.Equals("HQ", StringComparison.OrdinalIgnoreCase))
                    {
                        ViewBag.StationName = "HQ";
                    }
                    else
                    {
                        int stationId;
                        if (int.TryParse(employee.Station, out stationId))
                        {
                            var station = await _departmentService.GetStationByIdAsync(stationId);
                            ViewBag.StationName = station?.StationName ?? "Unknown";
                        }
                        else
                        {
                            ViewBag.StationName = employee.Station;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting station name for ID: {StationId}", employee.Station);
                    ViewBag.StationName = "Error retrieving name";
                }
            }
            else
            {
                ViewBag.StationName = "Not assigned";
            }

            // Get supervisor name
            if (!string.IsNullOrEmpty(employee.Supervisor))
            {
                try
                {
                    var supervisor = await _employeeService.GetEmployeeByPayrollAsync(employee.Supervisor);
                    ViewBag.SupervisorName = supervisor?.Fullname ?? "Unknown";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting supervisor name for payroll: {PayrollNo}", employee.Supervisor);
                    ViewBag.SupervisorName = "Error retrieving name";
                }
            }
            else
            {
                ViewBag.SupervisorName = "Not assigned";
            }

            // Get HOD name
            if (!string.IsNullOrEmpty(employee.Hod))
            {
                try
                {
                    var hod = await _employeeService.GetEmployeeByPayrollAsync(employee.Hod);
                    ViewBag.HodName = hod?.Fullname ?? "Unknown";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting HOD name for payroll: {PayrollNo}", employee.Hod);
                    ViewBag.HodName = "Error retrieving name";
                }
            }
            else
            {
                ViewBag.HodName = "Not assigned";
            }

            return View(employee);
        }

        // GET: Employees/Create
        public async Task<IActionResult> Create()
        {
            // Get departments for dropdown
            var departments = await _ktdaContext.Departments
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();
                
            // Get stations for dropdown
            var stations = await _ktdaContext.Stations
                .OrderBy(s => s.StationName)
                .ToListAsync();
                
            // Get roles for dropdown
            var roles = await _ktdaContext.EmployeeBkps
                .Where(e => !string.IsNullOrEmpty(e.Role))
                .Select(e => e.Role)
                .Distinct()
                .OrderBy(r => r)
                .ToListAsync();
                
            // Get employees for supervisor and HOD dropdowns
            var employees = await _ktdaContext.EmployeeBkps
                .Where(e => e.EmpisCurrActive == 0)
                .OrderBy(e => e.Fullname)
                .ToListAsync();
                
            ViewBag.Departments = departments;
            ViewBag.Stations = stations;
            ViewBag.Roles = roles;
            ViewBag.Employees = employees;
            
            // Generate a new RollNo (assuming it's a sequential number)
            var lastRollNo = await _ktdaContext.EmployeeBkps
                .OrderByDescending(e => e.RollNo)
                .Select(e => e.RollNo)
                .FirstOrDefaultAsync();
                
            int newRollNo = 1;
            if (!string.IsNullOrEmpty(lastRollNo) && int.TryParse(lastRollNo, out int lastNum))
            {
                newRollNo = lastNum + 1;
            }
            
            ViewBag.SuggestedRollNo = newRollNo.ToString().PadLeft(5, '0');
            
            return View();
        }

        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Designation,EmailAddress,SurName,OtherNames,PayrollNo,Station,PasswordP,Hod,Supervisor,Role,OtherName,Department,EmployeId,ContractEnd,HireDate,ServiceYears,Username,RetireDate,RollNo,Scale")] EmployeeBkp employee)
        {
            // Validate required fields
            ValidateRequiredFields(employee);
            
            if (ModelState.IsValid)
            {
                try
                {
                    // Set default values if not provided
                    employee.EmpisCurrActive = 0; // Active by default
                    
                    // Generate fullname from surname and other names
                    if (!string.IsNullOrEmpty(employee.SurName) && !string.IsNullOrEmpty(employee.OtherNames))
                    {
                        employee.Fullname = $"{employee.SurName} {employee.OtherNames}".Trim();
                    }
                    
                    // Check if employee with this payroll already exists
                    var existingEmployee = await _ktdaContext.EmployeeBkps
                        .AnyAsync(e => e.PayrollNo == employee.PayrollNo && e.RollNo == employee.RollNo);
                        
                    if (existingEmployee)
                    {
                        ModelState.AddModelError("PayrollNo", "An employee with this Payroll Number and Roll Number already exists.");
                        return await PrepareCreateView(employee);
                    }
                    
                    _ktdaContext.Add(employee);
                    await _ktdaContext.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating employee");
                    ModelState.AddModelError("", "An error occurred while creating the employee. Please try again.");
                    return await PrepareCreateView(employee);
                }
            }
            
            // If we got this far, something failed, redisplay form
            return await PrepareCreateView(employee);
        }
        
        // Helper method to validate required fields
        private void ValidateRequiredFields(EmployeeBkp employee)
        {
            // Validate personal information
            if (string.IsNullOrEmpty(employee.PayrollNo))
                ModelState.AddModelError("PayrollNo", "Payroll Number is required.");
            else if (employee.PayrollNo.Length != 8)
                ModelState.AddModelError("PayrollNo", "Payroll Number must be exactly 8 characters long.");
                
            if (string.IsNullOrEmpty(employee.RollNo))
                ModelState.AddModelError("RollNo", "Roll Number is required.");
                
            if (string.IsNullOrEmpty(employee.EmployeId))
                ModelState.AddModelError("EmployeId", "Employee ID Number is required.");
                
            if (string.IsNullOrEmpty(employee.SurName))
                ModelState.AddModelError("SurName", "Surname is required.");
                
            if (string.IsNullOrEmpty(employee.OtherNames))
                ModelState.AddModelError("OtherNames", "Other Names are required.");
                
            if (string.IsNullOrEmpty(employee.EmailAddress))
                ModelState.AddModelError("EmailAddress", "Email Address is required.");
                
            if (string.IsNullOrEmpty(employee.Username))
                ModelState.AddModelError("Username", "Username is required.");
            
            // Validate job information
            if (string.IsNullOrEmpty(employee.Designation))
                ModelState.AddModelError("Designation", "Designation is required.");
                
            if (string.IsNullOrEmpty(employee.Department))
                ModelState.AddModelError("Department", "Department is required.");
                
            if (string.IsNullOrEmpty(employee.Station))
                ModelState.AddModelError("Station", "Station is required.");
                
            if (string.IsNullOrEmpty(employee.Role))
                ModelState.AddModelError("Role", "Role is required.");
                
            if (string.IsNullOrEmpty(employee.Scale))
                ModelState.AddModelError("Scale", "Scale is required.");
            
            // Validate employment information
            if (employee.HireDate == default(DateTime))
                ModelState.AddModelError("HireDate", "Hire Date is required.");
            
            // Validate reporting structure
            if (string.IsNullOrEmpty(employee.Supervisor))
                ModelState.AddModelError("Supervisor", "Supervisor is required.");
                
            if (string.IsNullOrEmpty(employee.Hod))
                ModelState.AddModelError("Hod", "Head of Department is required.");
        }

        // Helper method to prepare Create view with all needed data
        private async Task<IActionResult> PrepareCreateView(EmployeeBkp employee)
        {
            // Get departments for dropdown
            var departments = await _ktdaContext.Departments
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();
                
            // Get stations for dropdown
            var stations = await _ktdaContext.Stations
                .OrderBy(s => s.StationName)
                .ToListAsync();
                
            // Get roles for dropdown
            var roles = await _ktdaContext.EmployeeBkps
                .Where(e => !string.IsNullOrEmpty(e.Role))
                .Select(e => e.Role)
                .Distinct()
                .OrderBy(r => r)
                .ToListAsync();
                
          
            // Get employees for supervisor and HOD dropdowns
            var employees = await _ktdaContext.EmployeeBkps
                .Where(e => e.EmpisCurrActive == 0)
                .OrderBy(e => e.Fullname)
                .ToListAsync();
                
            ViewBag.Departments = departments;
            ViewBag.Stations = stations;
            ViewBag.Roles = roles;
           
            ViewBag.Employees = employees;
            
            // We'll add the HQ option directly in the view
            
            return View(employee);
        }
        
        // API endpoint to get employees by department and/or station
        [HttpGet]
        public async Task<IActionResult> GetEmployeesByDepartment(string departmentId, string stationId = null)
        {
            if (string.IsNullOrEmpty(departmentId) && string.IsNullOrEmpty(stationId))
            {
                return BadRequest("Either Department ID or Station ID is required");
            }
            
            try
            {
                var query = _ktdaContext.EmployeeBkps.Where(e => e.EmpisCurrActive == 0);
                
                // Apply department filter if provided
                if (!string.IsNullOrEmpty(departmentId))
                {
                    query = query.Where(e => e.Department == departmentId);
                }
                
                // Apply station filter if provided
                if (!string.IsNullOrEmpty(stationId))
                {
                    // Handle special case for HQ
                    if (stationId.Equals("HQ", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(e => e.Station == "HQ" || e.Station.Contains("Head"));
                    }
                    else
                    {
                        // Try to match station ID with or without leading zeros
                        // For example, if stationId is "63", it should match "063" in the database
                        int stationIdNumber;
                        if (int.TryParse(stationId, out stationIdNumber))
                        {
                            // Use EF.Functions.Like for pattern matching
                            query = query.Where(e => e.Station == stationId || 
                                                   e.Station == stationIdNumber.ToString().PadLeft(3, '0'));
                        }
                        else
                        {
                            // If not a number, just use exact match
                            query = query.Where(e => e.Station == stationId);
                        }
                    }
                }
                
                var employees = await query
                    .Select(e => new
                    {
                        e.PayrollNo,
                        e.Fullname,
                        e.Designation,
                        e.Department,
                        e.Station
                    })
                    .OrderBy(e => e.Fullname)
                    .ToListAsync();
                    
                return Json(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employees for department {DepartmentId} and station {StationId}", departmentId, stationId);
                return StatusCode(500, "An error occurred while retrieving employees");
            }
        }

        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(string id, string rollNo)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(rollNo))
            {
                return NotFound();
            }

            var employee = await _ktdaContext.EmployeeBkps
                .FirstOrDefaultAsync(e => e.PayrollNo == id && e.RollNo == rollNo);
                
            if (employee == null)
            {
                return NotFound();
            }
            
            return await PrepareEditView(employee);
        }
        
        // Helper method to prepare Edit view with all needed data
        private async Task<IActionResult> PrepareEditView(EmployeeBkp employee)
        {
            // Get departments for dropdown
            var departments = await _ktdaContext.Departments
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();
                
            // Get stations for dropdown
            var stations = await _ktdaContext.Stations
                .OrderBy(s => s.StationName)
                .ToListAsync();
                
            // Get roles for dropdown
            var roles = await _ktdaContext.EmployeeBkps
                .Where(e => !string.IsNullOrEmpty(e.Role))
                .Select(e => e.Role)
                .Distinct()
                .OrderBy(r => r)
                .ToListAsync();
                
            // Get employees for supervisor and HOD dropdowns filtered by department and station if available
            var employeesQuery = _ktdaContext.EmployeeBkps
                .Where(e => e.EmpisCurrActive == 0 && e.PayrollNo != employee.PayrollNo);
                
            // Apply department filter if available
            if (!string.IsNullOrEmpty(employee.Department))
            {
                employeesQuery = employeesQuery.Where(e => e.Department == employee.Department);
            }
            
            // Apply station filter if available
            if (!string.IsNullOrEmpty(employee.Station))
            {
                // Handle special case for HQ
                if (employee.Station.Equals("HQ", StringComparison.OrdinalIgnoreCase))
                {
                    employeesQuery = employeesQuery.Where(e => e.Station == "HQ" || e.Station.Contains("Head"));
                }
                else
                {
                    // Try to match station ID with or without leading zeros
                    int stationIdNumber;
                    if (int.TryParse(employee.Station, out stationIdNumber))
                    {
                        employeesQuery = employeesQuery.Where(e => e.Station == employee.Station || 
                                                    e.Station == stationIdNumber.ToString().PadLeft(3, '0'));
                    }
                    else
                    {
                        employeesQuery = employeesQuery.Where(e => e.Station == employee.Station);
                    }
                }
            }
            
            var employees = await employeesQuery.OrderBy(e => e.Fullname).ToListAsync();
                
            ViewBag.Departments = departments;
            ViewBag.Stations = stations;
            ViewBag.Roles = roles;
            ViewBag.Employees = employees;
            
            return View(employee);
        }

        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, string rollNo, [Bind("Designation,EmailAddress,SurName,OtherNames,PayrollNo,Station,PasswordP,Hod,Supervisor,Role,OtherName,Department,EmployeId,ContractEnd,HireDate,ServiceYears,Username,RetireDate,RollNo,EmpisCurrActive,OrgGroup,Fullname,Pass,LastPay,Scale")] EmployeeBkp employee)
        {
            if (id != employee.PayrollNo || rollNo != employee.RollNo)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _ktdaContext.Update(employee);
                    await _ktdaContext.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.PayrollNo, employee.RollNo))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating employee");
                    ModelState.AddModelError("", "An error occurred while updating the employee. Please try again.");
                }
            }
            
            // If we got this far, something failed, redisplay form
            return await PrepareEditView(employee);
        }

        // GET: Employees/Delete/5
        public async Task<IActionResult> Delete(string id, string rollNo)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(rollNo))
            {
                return NotFound();
            }

            var employee = await _ktdaContext.EmployeeBkps
                .FirstOrDefaultAsync(m => m.PayrollNo == id && m.RollNo == rollNo);
                
            if (employee == null)
            {
                return NotFound();
            }

            // Get department name
            if (!string.IsNullOrEmpty(employee.Department))
            {
                try
                {
                    int departmentId;
                    if (int.TryParse(employee.Department, out departmentId))
                    {
                        var department = await _departmentService.GetDepartmentByIdAsync(departmentId);
                        ViewBag.DepartmentName = department?.DepartmentName ?? "Unknown";
                    }
                    else
                    {
                        ViewBag.DepartmentName = employee.Department;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting department name for ID: {DepartmentId}", employee.Department);
                    ViewBag.DepartmentName = "Error retrieving name";
                }
            }
            else
            {
                ViewBag.DepartmentName = "Not assigned";
            }

            // Get station name
            if (!string.IsNullOrEmpty(employee.Station))
            {
                try
                {
                    if (employee.Station.Equals("HQ", StringComparison.OrdinalIgnoreCase))
                    {
                        ViewBag.StationName = "HQ";
                    }
                    else
                    {
                        int stationId;
                        if (int.TryParse(employee.Station, out stationId))
                        {
                            var station = await _departmentService.GetStationByIdAsync(stationId);
                            ViewBag.StationName = station?.StationName ?? "Unknown";
                        }
                        else
                        {
                            ViewBag.StationName = employee.Station;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting station name for ID: {StationId}", employee.Station);
                    ViewBag.StationName = "Error retrieving name";
                }
            }
            else
            {
                ViewBag.StationName = "Not assigned";
            }

            return View(employee);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id, string rollNo)
        {
            var employee = await _ktdaContext.EmployeeBkps
                .FirstOrDefaultAsync(m => m.PayrollNo == id && m.RollNo == rollNo);
                
            if (employee != null)
            {
                try
                {
                    // Instead of deleting, mark as inactive
                    employee.EmpisCurrActive = 1; // Assuming 1 means inactive
                    _ktdaContext.Update(employee);
                    await _ktdaContext.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deactivating employee with PayrollNo {PayrollNo}", id);
                    ModelState.AddModelError("", "An error occurred while deactivating the employee. It may be referenced by other records.");
                    
                    // Get department name
                    if (!string.IsNullOrEmpty(employee.Department))
                    {
                        try
                        {
                            int departmentId;
                            if (int.TryParse(employee.Department, out departmentId))
                            {
                                var department = await _departmentService.GetDepartmentByIdAsync(departmentId);
                                ViewBag.DepartmentName = department?.DepartmentName ?? "Unknown";
                            }
                            else
                            {
                                ViewBag.DepartmentName = employee.Department;
                            }
                        }
                        catch
                        {
                            ViewBag.DepartmentName = "Error retrieving name";
                        }
                    }
                    else
                    {
                        ViewBag.DepartmentName = "Not assigned";
                    }
                    
                    // Get station name
                    if (!string.IsNullOrEmpty(employee.Station))
                    {
                        try
                        {
                            if (employee.Station.Equals("HQ", StringComparison.OrdinalIgnoreCase))
                            {
                                ViewBag.StationName = "HQ";
                            }
                            else
                            {
                                int stationId;
                                if (int.TryParse(employee.Station, out stationId))
                                {
                                    var station = await _departmentService.GetStationByIdAsync(stationId);
                                    ViewBag.StationName = station?.StationName ?? "Unknown";
                                }
                                else
                                {
                                    ViewBag.StationName = employee.Station;
                                }
                            }
                        }
                        catch
                        {
                            ViewBag.StationName = "Error retrieving name";
                        }
                    }
                    else
                    {
                        ViewBag.StationName = "Not assigned";
                    }
                    
                    return View(employee);
                }
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(string payrollNo, string rollNo)
        {
            return _ktdaContext.EmployeeBkps.Any(e => e.PayrollNo == payrollNo && e.RollNo == rollNo);
        }
        
        // Helper method to apply sorting to the query
        private IQueryable<EmployeeBkp> ApplySorting(IQueryable<EmployeeBkp> query, string sortField, string sortOrder)
        {
            // Default to sorting by Fullname if sortField is invalid
            if (string.IsNullOrEmpty(sortField))
            {
                sortField = "Fullname";
            }
            
            // Determine if sorting ascending or descending
            bool isAscending = string.IsNullOrEmpty(sortOrder) || sortOrder.ToLower() == "asc";
            
            // Apply sorting based on the field
            switch (sortField.ToLower())
            {
                case "payrollno":
                    query = isAscending ? query.OrderBy(e => e.PayrollNo) : query.OrderByDescending(e => e.PayrollNo);
                    break;
                case "fullname":
                    query = isAscending ? query.OrderBy(e => e.Fullname) : query.OrderByDescending(e => e.Fullname);
                    break;
                case "department":
                    query = isAscending ? query.OrderBy(e => e.Department) : query.OrderByDescending(e => e.Department);
                    break;
                case "station":
                    query = isAscending ? query.OrderBy(e => e.Station) : query.OrderByDescending(e => e.Station);
                    break;
                case "designation":
                    query = isAscending ? query.OrderBy(e => e.Designation) : query.OrderByDescending(e => e.Designation);
                    break;
                case "role":
                    query = isAscending ? query.OrderBy(e => e.Role) : query.OrderByDescending(e => e.Role);
                    break;
                case "hiredate":
                    query = isAscending ? query.OrderBy(e => e.HireDate) : query.OrderByDescending(e => e.HireDate);
                    break;
                default:
                    query = isAscending ? query.OrderBy(e => e.Fullname) : query.OrderByDescending(e => e.Fullname);
                    break;
            }
            
            return query;
        }

        // Helper method to apply sorting to the view query
        private IQueryable<EmployeeDetailsView> ApplyViewSorting(IQueryable<EmployeeDetailsView> query, string sortField, string sortOrder)
        {
            // Default to sorting by Fullname if sortField is invalid
            if (string.IsNullOrEmpty(sortField))
            {
                sortField = "Fullname";
            }

            // Determine if sorting ascending or descending
            bool isAscending = string.IsNullOrEmpty(sortOrder) || sortOrder.ToLower() == "asc";

            // Apply sorting based on the field - map old field names to view properties
            switch (sortField.ToLower())
            {
                case "payrollno":
                    query = isAscending ? query.OrderBy(e => e.PayrollNo) : query.OrderByDescending(e => e.PayrollNo);
                    break;
                case "fullname":
                    query = isAscending ? query.OrderBy(e => e.Fullname) : query.OrderByDescending(e => e.Fullname);
                    break;
                case "department":
                    query = isAscending ? query.OrderBy(e => e.DepartmentName) : query.OrderByDescending(e => e.DepartmentName);
                    break;
                case "station":
                    query = isAscending ? query.OrderBy(e => e.StationName) : query.OrderByDescending(e => e.StationName);
                    break;
                case "designation":
                    query = isAscending ? query.OrderBy(e => e.Designation) : query.OrderByDescending(e => e.Designation);
                    break;
                case "role":
                    query = isAscending ? query.OrderBy(e => e.Role) : query.OrderByDescending(e => e.Role);
                    break;
                default:
                    query = isAscending ? query.OrderBy(e => e.Fullname) : query.OrderByDescending(e => e.Fullname);
                    break;
            }

            return query;
        }

        // GET: Profile - Show logged-in user's profile
        public async Task<IActionResult> Profile()
        {
            try
            {
                // Get the logged-in user's payroll number from session
                var payrollNo = HttpContext.Session.GetString("EmployeePayrollNo");
                
                if (string.IsNullOrEmpty(payrollNo))
                {
                    _logger.LogWarning("No payroll number found in session for profile request");
                    return RedirectToAction("Login", "Account");
                }

                // Get the employee to find the roll number
                var employee = await _ktdaContext.EmployeeBkps
                    .FirstOrDefaultAsync(m => m.PayrollNo == payrollNo);
                    
                if (employee == null)
                {
                    _logger.LogWarning("Employee not found for payroll number: {PayrollNo}", payrollNo);
                    return NotFound("Employee profile not found.");
                }

                // Redirect to the existing Details action with the user's payroll and roll number
                return RedirectToAction("Details", new { id = employee.PayrollNo, rollNo = employee.RollNo });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user profile");
                return View("Error");
            }
        }
    }
}
