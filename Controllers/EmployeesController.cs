using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MRIV.Attributes;
using MRIV.Extensions;
using MRIV.Models;
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
        private readonly KtdaleaveContext _ktdaContext;
        private readonly IEmployeeService _employeeService;
        private readonly IDepartmentService _departmentService;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(
            KtdaleaveContext ktdaContext,
            IEmployeeService employeeService,
            IDepartmentService departmentService,
            ILogger<EmployeesController> logger)
        {
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
            
            // Start with base query for employees - include all employees (both active and inactive)
            // Use AsNoTracking for read-only operations to improve performance
            var employeesQuery = _ktdaContext.EmployeeBkps
                .AsNoTracking()
                .AsQueryable();
                
            // Get filter values from request query string
            var filters = new Dictionary<string, string>();
            foreach (var key in Request.Query.Keys.Where(k => k != "page" && k != "pageSize" && k != "sortField" && k != "sortOrder" && k != "searchTerm"))
            {
                // Skip if the value is null or empty
                if (!string.IsNullOrEmpty(Request.Query[key]))
                {
                    filters[key] = Request.Query[key];
                }
            }
            
            // Get department names for filters - use AsNoTracking for better performance
            var departmentsList = await _ktdaContext.Departments
                .AsNoTracking()
                .Select(d => new { Id = d.DepartmentId.ToString(), Name = d.DepartmentName })
                .ToListAsync();
            var allDepartments = departmentsList
                .GroupBy(d => d.Id)
                .ToDictionary(g => g.Key, g => g.First().Name);
            
            // Get station names for filters - use AsNoTracking for better performance
            var stationsList = await _ktdaContext.Stations
                .AsNoTracking()
                .Select(s => new { Id = s.StationId.ToString(), Name = s.StationName })
                .ToListAsync();
                
            // Normalize station names - remove any existing "Head Office" entries to avoid duplicates
            var headOfficeEntries = stationsList
                .Where(s => s.Name.Contains("Head Office", StringComparison.OrdinalIgnoreCase) ||
                            s.Name.Contains("Headoffice", StringComparison.OrdinalIgnoreCase) ||
                            s.Name.Contains("HQ", StringComparison.OrdinalIgnoreCase) ||
                            s.Id == "0" ||
                            s.Id == "HQ")
                .ToList();
                
            foreach (var entry in headOfficeEntries)
            {
                stationsList.Remove(entry);
            }
            
            var allStations = stationsList
                .GroupBy(s => s.Id)
                .ToDictionary(g => g.Key, g => g.First().Name);
                
            // Add single standardized HQ entry
            allStations["HQ"] = "Head Office (HQ)";
            allStations["0"] = "Head Office (HQ)";
            
            // Get distinct station values from employees table to ensure we have the exact format used in the data
            // Optimize by using Take(1000) to prevent excessive query time on large datasets
            var distinctStations = await employeesQuery
                .Where(e => !string.IsNullOrEmpty(e.Station))
                .Select(e => e.Station)
                .OrderBy(s => s)  // Add explicit ordering before pagination
                .Distinct()
                .Take(1000)  // Limit to prevent excessive query time
                .ToListAsync();
                
            // Create a more complete station dictionary that includes both formatted and unformatted IDs
            var completeStationDict = new Dictionary<string, string>();
            
            // Add all stations from the stations table
            foreach (var station in allStations)
            {
                completeStationDict[station.Key] = station.Value;
                
                // For numeric IDs, also add versions with/without leading zeros
                if (int.TryParse(station.Key, out int stationId))
                {
                    string paddedId = stationId.ToString().PadLeft(3, '0');
                    if (!completeStationDict.ContainsKey(paddedId))
                    {
                        completeStationDict[paddedId] = station.Value;
                    }
                }
            }
            
            // Add HQ option
            if (!completeStationDict.ContainsKey("HQ"))
            {
                completeStationDict["HQ"] = "Head Office (HQ)";
            }
            
            // Add any stations from the employee table that might not be in the stations table
            foreach (var stationId in distinctStations)
            {
                if (!completeStationDict.ContainsKey(stationId))
                {
                    completeStationDict[stationId] = $"Station {stationId}";
                }
            }
            
            // Create a dictionary for display names
            var displayNames = new Dictionary<string, Dictionary<string, string>>();
            
            // Only add dictionaries if they have values
            if (allDepartments.Any())
            {
                displayNames.Add("Department", allDepartments);
            }
            
            if (completeStationDict.Any())
            {
                displayNames.Add("Station", completeStationDict);
            }
            
            // Create filter view model with explicit type for the array
            ViewBag.Filters = await employeesQuery.CreateFiltersAsync(
                new Expression<Func<EmployeeBkp, object>>[] {
                    // Select which properties to create filters for - Station first
                    e => e.Station,
                    e => e.Department,
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
                    (e.EmployeId != null && e.EmployeId.ToLower().Contains(searchTerm)) ||
                    (e.Designation != null && e.Designation.ToLower().Contains(searchTerm)) ||
                    (e.EmailAddress != null && e.EmailAddress.ToLower().Contains(searchTerm)));
            }

            // Get total count for pagination after filtering
            var totalEmployees = await employeesQuery.CountAsync();

            // Apply sorting
            employeesQuery = ApplySorting(employeesQuery, sortField, sortOrder);
            
            // If no sort field is provided, add a default sort to ensure consistent pagination
            if (string.IsNullOrEmpty(sortField))
            {
                employeesQuery = employeesQuery.OrderBy(e => e.Fullname).ThenBy(e => e.PayrollNo);
            }

            // Apply pagination
            var employees = await employeesQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Create query for distinct departments in the filtered employee list (for the filter dropdown)
            // Optimize by using Take(1000) to prevent excessive query time on large datasets
            var distinctDepartments = await employeesQuery
                .Where(e => !string.IsNullOrEmpty(e.Department))
                .Select(e => e.Department)
                .OrderBy(d => d)  // Add explicit ordering before pagination
                .Distinct()
                .Take(1000)  // Limit to prevent excessive query time
                .ToListAsync();
                
            // Get department names for display
            var departmentIds = employees.Where(e => !string.IsNullOrEmpty(e.Department))
                .Select(e => e.Department)
                .OrderBy(d => d)  // Add explicit ordering before pagination
                .Distinct()
                .ToList();
                
            var departmentNames = new Dictionary<string, string>();
            foreach (var deptId in departmentIds)
            {
                if (!string.IsNullOrEmpty(deptId) && !departmentNames.ContainsKey(deptId))
                {
                    try
                    {
                        int departmentId;
                        if (int.TryParse(deptId, out departmentId))
                        {
                            var department = await _departmentService.GetDepartmentByIdAsync(departmentId);
                            departmentNames[deptId] = department?.DepartmentName ?? "Unknown";
                        }
                        else
                        {
                            departmentNames[deptId] = deptId;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting department name for ID: {DepartmentId}", deptId);
                        departmentNames[deptId] = "Error";
                    }
                }
            }

            // Get station names for display
            var stationIds = employees.Where(e => !string.IsNullOrEmpty(e.Station))
                .Select(e => e.Station)
                .Distinct()
                .ToList();
                
            var stationNames = new Dictionary<string, string>();
            foreach (var stationId in stationIds)
            {
                if (!string.IsNullOrEmpty(stationId) && !stationNames.ContainsKey(stationId))
                {
                    try
                    {
                        // Handle HQ special case
                        if (stationId.Equals("HQ", StringComparison.OrdinalIgnoreCase))
                        {
                            stationNames[stationId] = "HQ";
                        }
                        else
                        {
                            int sId;
                            if (int.TryParse(stationId, out sId))
                            {
                                var station = await _departmentService.GetStationByIdAsync(sId);
                                stationNames[stationId] = station?.StationName ?? "Unknown";
                            }
                            else
                            {
                                stationNames[stationId] = stationId;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting station name for ID: {StationId}", stationId);
                        stationNames[stationId] = "Error";
                    }
                }
            }

            // Set up pagination view model
            var paginationViewModel = new PaginationViewModel
            {
                CurrentPage = page,
                TotalItems = totalEmployees,
                ItemsPerPage = pageSize,
                Action = "Index",
                Controller = "Employees"
            };

            // Pass data to view
            ViewBag.DepartmentNames = departmentNames;
            ViewBag.StationNames = stationNames;
            ViewBag.Pagination = paginationViewModel;
            ViewBag.TotalCount = totalEmployees;
            ViewBag.SortField = sortField;
            ViewBag.SortOrder = sortOrder;
            ViewBag.SearchTerm = searchTerm;

            // Create view models for the employees
            var viewModels = new List<EmployeeViewModel>();
            foreach (var employee in employees)
            {
                viewModels.Add(new EmployeeViewModel
                {
                    PayrollNo = employee.PayrollNo,
                    RollNo = employee.RollNo,
                    EmployeeId = employee.EmployeId,
                    FullName = employee.Fullname,
                    Department = employee.Department,
                    DepartmentName = departmentNames.ContainsKey(employee.Department) ? departmentNames[employee.Department] : "Unknown",
                    Station = employee.Station,
                    StationName = stationNames.ContainsKey(employee.Station) ? stationNames[employee.Station] : "Unknown",
                    Role = employee.Role,
                    Designation = employee.Designation,
                    EmailAddress = employee.EmailAddress,
                    EmpisCurrActive = (int)employee.EmpisCurrActive
                });
            }

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
