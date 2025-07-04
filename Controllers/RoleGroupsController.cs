using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MRIV.Attributes;
using MRIV.Models;
using MRIV.Services;
using MRIV.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MRIV.Controllers
{
    [CustomAuthorize]
    public class RoleGroupsController : Controller
    {
        private readonly RequisitionContext _context;
        private readonly KtdaleaveContext _ktdaContext;
        private readonly Services.IEmployeeService _employeeService;
        private readonly Services.IDepartmentService _departmentService;
        private readonly ILogger<RoleGroupsController> _logger;

        public RoleGroupsController(
            RequisitionContext context,
            KtdaleaveContext ktdaContext,
            Services.IEmployeeService employeeService,
            Services.IDepartmentService departmentService,
            ILogger<RoleGroupsController> logger)
        {
            _context = context;
            _ktdaContext = ktdaContext;
            _employeeService = employeeService;
            _departmentService = departmentService;
            _logger = logger;
        }

        // GET: RoleGroups
        public async Task<IActionResult> Index()
        {
            var roleGroups = await _context.RoleGroups
                .OrderByDescending(rg => rg.CreatedAt)
                .ToListAsync();

            // Get member counts for each role group
            var memberCounts = new Dictionary<int, int>();
            foreach (var roleGroup in roleGroups)
            {
                var count = await _context.RoleGroupMembers
                    .CountAsync(m => m.RoleGroupId == roleGroup.Id && m.IsActive);
                memberCounts[roleGroup.Id] = count;
            }

            ViewBag.MemberCounts = memberCounts;
            return View("~/Views/Roles/RoleGroups.cshtml", roleGroups);
        }

        // GET: RoleGroups/Details/{id}
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var roleGroup = await _context.RoleGroups
                .FirstOrDefaultAsync(m => m.Id == id);

            if (roleGroup == null)
            {
                return NotFound();
            }

            // Get members of this role group
            var members = await _context.RoleGroupMembers
                .Where(m => m.RoleGroupId == id && m.IsActive)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var memberViewModels = new List<RoleGroupMemberViewModel>();
            foreach (var member in members)
            {
                var employee = await _employeeService.GetEmployeeByPayrollAsync(member.PayrollNo);
                if (employee != null)
                {
                    memberViewModels.Add(new RoleGroupMemberViewModel
                    {
                        Id = member.Id,
                        RoleGroupId = member.RoleGroupId,
                        PayrollNo = member.PayrollNo,
                        EmployeeName = employee.Fullname,
                        Department = employee.Department,
                        Station = employee.Station,
                        Designation = employee.Designation,
                       
                    });
                }
            }

            ViewBag.RoleGroup = roleGroup;
            return View("~/Views/Roles/RoleGroupDetails.cshtml", memberViewModels);
        }

        // GET: RoleGroups/Create
        public IActionResult Create()
        {
            return View("~/Views/Roles/CreateRoleGroup.cshtml");
        }

        // POST: RoleGroups/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int id, [Bind("Id,Name,Description,CanAccessAcrossDepartments,CanAccessAcrossStations,IsActive")] RoleGroup roleGroup)
        {
            if (ModelState.IsValid)
            {
                roleGroup.CreatedAt = DateTime.Now;
                roleGroup.UpdatedAt = DateTime.Now;
                roleGroup.CanAccessAcrossDepartments = roleGroup.CanAccessAcrossDepartments;
                roleGroup.CanAccessAcrossStations = roleGroup.CanAccessAcrossStations;
                roleGroup.IsActive = roleGroup.IsActive;

                _context.Add(roleGroup);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View("~/Views/Roles/CreateRoleGroup.cshtml", roleGroup);
        }

        // GET: RoleGroups/Edit/{id}
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var roleGroup = await _context.RoleGroups.FindAsync(id);
            if (roleGroup == null)
            {
                return NotFound();
            }
            return View("~/Views/Roles/EditRoleGroup.cshtml", roleGroup);
        }

        // POST: RoleGroups/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
     public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,CanAccessAcrossDepartments,CanAccessAcrossStations,IsActive")] RoleGroup roleGroup)
        {
            if (id != roleGroup.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingRoleGroup = await _context.RoleGroups.FindAsync(id);
                    if (existingRoleGroup == null)
                    {
                        return NotFound();
                    }

                    existingRoleGroup.Name = roleGroup.Name;
                    existingRoleGroup.Description = roleGroup.Description;
                    existingRoleGroup.CanAccessAcrossDepartments = roleGroup.CanAccessAcrossDepartments;
                    existingRoleGroup.CanAccessAcrossStations = roleGroup.CanAccessAcrossStations;
                    existingRoleGroup.IsActive = roleGroup.IsActive;
                    existingRoleGroup.UpdatedAt = DateTime.Now;

                    _context.Update(existingRoleGroup);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoleGroupExists(roleGroup.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View("~/Views/Roles/EditRoleGroup.cshtml", roleGroup);
        }
        // GET: RoleGroups/Delete/{id}
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var roleGroup = await _context.RoleGroups
                .FirstOrDefaultAsync(m => m.Id == id);
            if (roleGroup == null)
            {
                return NotFound();
            }

            return View("~/Views/Roles/DeleteRoleGroup.cshtml", roleGroup);
        }

        // POST: RoleGroups/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var roleGroup = await _context.RoleGroups.FindAsync(id);
            if (roleGroup != null)
            {
                // Soft delete
                roleGroup.IsActive = false;
                roleGroup.UpdatedAt = DateTime.Now;
                _context.Update(roleGroup);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        // GET: RoleGroups/AddMember/{roleGroupId}
        public async Task<IActionResult> AddMember(int id)
        {
            var roleGroup = await _context.RoleGroups.FindAsync(id);
            if (roleGroup == null)
            {
                return NotFound();
            }

            ViewBag.RoleGroup = roleGroup;

            var viewModel = new AddRoleGroupMemberViewModel
            {
                RoleGroupId = roleGroup.Id,
                RoleGroupName = roleGroup.Name
            };

            // Add empty first items to dropdowns
            viewModel.Stations.Add(new SelectListItem { Value = "", Text = "-- Select Station --" });
            viewModel.Departments.Add(new SelectListItem { Value = "", Text = "-- Select Department --" });
            viewModel.Roles.Add(new SelectListItem { Value = "", Text = "-- Select Role --" });
            
            // Populate stations
            var stationsList = await _ktdaContext.Stations
                .AsNoTracking()
                .Select(s => new { Value = s.StationId.ToString(), Text = s.StationName })
                .OrderBy(s => s.Text)
                .ToListAsync();
            foreach (var station in stationsList)
            {
                viewModel.Stations.Add(new SelectListItem { Value = station.Value, Text = station.Text });
            }

            // Populate departments
            var departmentsList = await _ktdaContext.Departments
                .AsNoTracking()
                .Select(d => new { Value = d.DepartmentId.ToString(), Text = d.DepartmentName })
                .OrderBy(d => d.Text)
                .ToListAsync();
            foreach (var dept in departmentsList)
            {
                viewModel.Departments.Add(new SelectListItem { Value = dept.Value, Text = dept.Text });
            }
            
            // Get roles for dropdown
            var roleOptions = await _ktdaContext.EmployeeBkps
                .Where(e => !string.IsNullOrEmpty(e.Role))
                .Select(e => e.Role)
                .Distinct()
                .OrderBy(r => r)
                .ToListAsync();
                
            foreach (var role in roleOptions)
            {
                viewModel.Roles.Add(new SelectListItem { Value = role, Text = role });
            }
            
            // Load initial employees (10 active employees)
            var initialEmployees = await _ktdaContext.EmployeeBkps
                .Where(e => e.EmpisCurrActive == 0)
                .OrderBy(e => e.PayrollNo)
                .Take(10)
                .ToListAsync();
                
            // Get department IDs for display
            var departmentIds = initialEmployees.Where(e => !string.IsNullOrEmpty(e.Department))
                .Select(e => e.Department)
                .Distinct()
                .ToList();
                
            // Create department name lookup dictionary
            var departmentNames = new Dictionary<string, string>();
            foreach (var deptId in departmentIds)
            {
                if (!string.IsNullOrEmpty(deptId) && !departmentNames.ContainsKey(deptId))
                {
                    try
                    {
                        var department = await _departmentService.GetDepartmentByNameAsync(deptId);
                        departmentNames[deptId] = department?.DepartmentName ?? deptId;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting department name for ID: {DepartmentId}", deptId);
                        departmentNames[deptId] = deptId;
                    }
                }
            }
            
            // Map employees to view model
            var employeeViewModels = initialEmployees.Select(e => new EmployeeSearchResultViewModel
            {
                PayrollNo = e.PayrollNo,
                Name = e.Fullname ?? "Unknown",
                Designation = e.Designation ?? "N/A",
                Department = departmentNames.ContainsKey(e.Department) ? departmentNames[e.Department] : e.Department,
                Role = e.Role ?? "N/A"
            }).ToList();

            ViewBag.InitialEmployees = employeeViewModels;

            return View("~/Views/Roles/AddMember.cshtml", viewModel);
        }

        // POST: RoleGroups/AddMember
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(AddRoleGroupMemberViewModel model)
        {
            if (ModelState.IsValid && model.PayrollNos != null && model.PayrollNos.Any())
            {
                foreach (var payrollNo in model.PayrollNos.Distinct())
                {
                    var employee = await _employeeService.GetEmployeeByPayrollAsync(payrollNo);
                    if (employee == null)
                        continue; // Optionally collect errors

                    var existingMember = await _context.RoleGroupMembers
                        .FirstOrDefaultAsync(m => m.RoleGroupId == model.RoleGroupId &&
                                                 m.PayrollNo == payrollNo &&
                                                 m.IsActive);

                    if (existingMember != null)
                        continue; // Optionally collect errors

                    var roleGroupMember = new RoleGroupMember
                    {
                        RoleGroupId = model.RoleGroupId,
                        PayrollNo = payrollNo,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        IsActive = true
                    };

                    _context.Add(roleGroupMember);
                }
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = model.RoleGroupId });
            }
            // If no valid payroll numbers, redisplay form with error
            ModelState.AddModelError("PayrollNos", "Please select at least one employee to add.");
            return View("~/Views/Roles/AddMember.cshtml", model);
        }

        // POST: RoleGroups/RemoveMember/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int id)
        {
            var member = await _context.RoleGroupMembers.FindAsync(id);
            if (member != null)
            {
                // Soft delete
                member.IsActive = false;
                member.UpdatedAt = DateTime.Now;
                _context.Update(member);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Details), new { id = member.RoleGroupId });
        }

        // GET: RoleGroups/SearchEmployees
        [HttpGet]
        public async Task<IActionResult> SearchEmployees(string stationCategory, string stationId, string departmentId, string role, string searchTerm)
        {
            var employeesQuery = _ktdaContext.EmployeeBkps.AsQueryable();

            // Apply active employees filter
            employeesQuery = employeesQuery.Where(e => e.EmpisCurrActive == 0);

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
            var distinctStations = await _ktdaContext.EmployeeBkps
                .Where(e => !string.IsNullOrEmpty(e.Station))
                .Select(e => e.Station)
                .Distinct()
                .Take(100) // Limit to 100 distinct stations for performance
                .ToListAsync();

            var completeStationDict = new Dictionary<string, string>();

            // Add all stations from the stations table
            foreach (var station in allStations)
            {
                completeStationDict[station.Key] = station.Value;

                // For numeric IDs, also add versions with/without leading zeros
                if (int.TryParse(station.Key, out int stationIdInt))
                {
                    string paddedId = stationIdInt.ToString().PadLeft(3, '0');
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
            foreach (var stationIdValue in distinctStations)
            {
                if (!completeStationDict.ContainsKey(stationIdValue))
                {
                    completeStationDict[stationIdValue] = $"Station {stationIdValue}";
                }
            }

            // Get distinct roles for filter dropdown
            var distinctRoles = await _ktdaContext.EmployeeBkps
                .Where(e => !string.IsNullOrEmpty(e.Role))
                .Select(e => e.Role)
                .Distinct()
                .Take(100) // Limit to 100 distinct roles for performance
                .ToListAsync();

            // Apply station category filter
            if (!string.IsNullOrEmpty(stationCategory))
            {
                switch (stationCategory.ToLower())
                {
                    case "hq":
                        employeesQuery = employeesQuery.Where(e => e.Station == "HQ" || e.Station == "0");
                        break;
                    case "factory":
                        employeesQuery = employeesQuery.Where(e =>
                            !string.IsNullOrEmpty(e.Station) &&
                            e.Station != "HQ" && e.Station != "0" &&
                            e.Station.All(char.IsDigit) &&
                            Convert.ToInt32(e.Station) > 0 && 
                            Convert.ToInt32(e.Station) < 100);
                        break;
                    case "region":
                        employeesQuery = employeesQuery.Where(e =>
                            !string.IsNullOrEmpty(e.Station) &&
                            e.Station != "HQ" && e.Station != "0" &&
                            e.Station.All(char.IsDigit) &&
                            Convert.ToInt32(e.Station) >= 100);
                        break;
                }
            }

            // Apply station filter
            if (!string.IsNullOrEmpty(stationId))
            {
                var stationName = completeStationDict.ContainsKey(stationId) ? completeStationDict[stationId] : null;
                string paddedStationId = stationId.PadLeft(3, '0');
                if (!string.IsNullOrEmpty(stationName))
                    employeesQuery = employeesQuery.Where(e =>
                        e.Station == stationName ||
                        e.Station == stationId ||
                        e.Station == paddedStationId
                    );
                else
                    employeesQuery = employeesQuery.Where(e =>
                        e.Station == stationId ||
                        e.Station == paddedStationId
                    );
            }

            // Apply department filter
            if (!string.IsNullOrEmpty(departmentId))
            {
                var departmentName = allDepartments.ContainsKey(departmentId) ? allDepartments[departmentId] : null;
                if (!string.IsNullOrEmpty(departmentName))
                    employeesQuery = employeesQuery.Where(e => e.Department == departmentName || e.Department == departmentId);
                else
                    employeesQuery = employeesQuery.Where(e => e.Department == departmentId);
            }

            // Apply role filter
            if (!string.IsNullOrEmpty(role))
            {
                employeesQuery = employeesQuery.Where(e => e.Role == role);
            }

            // Apply search term filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                employeesQuery = employeesQuery.Where(e =>
                    e.PayrollNo.Contains(searchTerm) ||
                    e.Fullname.Contains(searchTerm) ||
                    (e.Role != null && e.Role.Contains(searchTerm)) ||
                    (e.Designation != null && e.Designation.Contains(searchTerm))
                );
            }

            // Add filter data to ViewBag
            ViewBag.DepartmentNames = allDepartments;
            ViewBag.StationNames = completeStationDict;
            ViewBag.Roles = distinctRoles;
            ViewBag.StationCategories = new List<string> { "HQ", "Factory", "Region" };

            // Store current filter values in ViewBag
            ViewBag.StationCategoryFilter = stationCategory;
            ViewBag.StationFilter = stationId;
            ViewBag.DepartmentFilter = departmentId;
            ViewBag.RoleFilter = role;
            ViewBag.SearchTerm = searchTerm;

            // Get employees with limit for performance
            var employees = await employeesQuery
                .OrderBy(e => e.PayrollNo)
                .Take(100)
                .ToListAsync();
                
            // Map employees to view model - using the department dictionary we already built above
            var results = employees.Select(e => new EmployeeSearchResultViewModel
            {
                PayrollNo = e.PayrollNo,
                Name = e.Fullname ?? "Unknown",
                Designation = e.Designation ?? "N/A",
                Department = !string.IsNullOrEmpty(e.Department) && allDepartments.ContainsKey(e.Department)
                    ? allDepartments[e.Department] 
                    : e.Department ?? "N/A",
                Role = e.Role ?? "N/A"
            }).ToList();

            return Json(new { 
                results,
                departmentNames = allDepartments,
                stationNames = completeStationDict,
                roles = distinctRoles,
                stationCategories = new List<string> { "HQ", "Factory", "Region" },
                filters = new {
                    stationCategory,
                    stationId,
                    departmentId,
                    role,
                    searchTerm
                }
            });
        }

        private bool RoleGroupExists(int id)
        {
            return _context.RoleGroups.Any(e => e.Id == id);
        }
    }
}
