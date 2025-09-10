using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MRIV.Attributes;
using MRIV.Models;
using MRIV.Services;
using MRIV.ViewModels;
using MRIV.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
        private readonly IVisibilityAuthorizeService _visibilityService;
        private readonly ILogger<RoleGroupsController> _logger;

        public RoleGroupsController(
            RequisitionContext context,
            KtdaleaveContext ktdaContext,
            Services.IEmployeeService employeeService,
            Services.IDepartmentService departmentService,
            IVisibilityAuthorizeService visibilityService,
            ILogger<RoleGroupsController> logger)
        {
            _context = context;
            _ktdaContext = ktdaContext;
            _employeeService = employeeService;
            _departmentService = departmentService;
            _visibilityService = visibilityService;
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
        public async Task<IActionResult> AddMember(int id, int page = 1, int pageSize = 100, string searchTerm = "")
        {
            var roleGroup = await _context.RoleGroups.FindAsync(id);
            if (roleGroup == null)
            {
                return NotFound();
            }

            // Get current user's payroll number from session
            var userPayrollNo = HttpContext.Session.GetString("EmployeePayrollNo");
            if (string.IsNullOrEmpty(userPayrollNo))
                return RedirectToAction("Index", "Login");

            ViewBag.RoleGroup = roleGroup;

            // Get filter values from request query string
            var stationFilter = Request.Query["Station"].ToString();
            var departmentFilter = Request.Query["Department"].ToString();
            var roleFilter = Request.Query["Role"].ToString();

            // Get visible departments and stations for current user
            var visibleDepartments = await _visibilityService.GetVisibleDepartmentsAsync(userPayrollNo);
            var visibleStations = await _visibilityService.GetVisibleStationsAsync(userPayrollNo);

            // Get all active employees first
            var allEmployees = await _ktdaContext.EmployeeBkps
                .Where(e => e.EmpisCurrActive == 0)
                .ToListAsync();

            // Apply visibility filtering in memory
            var filteredEmployees = allEmployees.AsEnumerable();

            if (visibleDepartments.Any())
            {
                var visibleDeptIds = visibleDepartments.Select(d => d.DepartmentId.ToString()).ToList();
                filteredEmployees = filteredEmployees.Where(e => visibleDeptIds.Contains(e.Department));
            }

            if (visibleStations.Any())
            {
                var visibleStationIds = visibleStations.Select(s => s.StationId.ToString()).ToList();
                visibleStationIds.Add("HQ");
                visibleStationIds.Add("0");
                filteredEmployees = filteredEmployees.Where(e => visibleStationIds.Contains(e.Station));
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
                filteredEmployees = filteredEmployees.Where(e => 
                    (e.Fullname != null && e.Fullname.ToLower().Contains(searchTerm)) || 
                    (e.PayrollNo != null && e.PayrollNo.ToLower().Contains(searchTerm)) ||
                    (e.Designation != null && e.Designation.ToLower().Contains(searchTerm)));
            }

            // Apply additional filters
            if (!string.IsNullOrEmpty(stationFilter))
            {
                filteredEmployees = filteredEmployees.Where(e => e.Station == stationFilter);
            }
            
            if (!string.IsNullOrEmpty(departmentFilter))
            {
                filteredEmployees = filteredEmployees.Where(e => e.Department == departmentFilter);
            }
            
            if (!string.IsNullOrEmpty(roleFilter))
            {
                filteredEmployees = filteredEmployees.Where(e => e.Role == roleFilter);
            }

            // Create display name mappings
            var departmentNames = visibleDepartments
                .GroupBy(d => d.DepartmentId.ToString())
                .ToDictionary(g => g.Key, g => g.First().DepartmentName);
            var stationNames = visibleStations
                .GroupBy(s => s.StationId.ToString())
                .ToDictionary(g => g.Key, g => g.First().StationName);
            stationNames["HQ"] = "Head Office (HQ)";
            stationNames["0"] = "Head Office (HQ)";

            // Create filter view model
            var filterViewModel = new FilterViewModel();
            
            var stationFilterDef = new FilterDefinition
            {
                PropertyName = "Station",
                DisplayName = "Station",
                Options = stationNames.Select(s => new SelectListItem 
                { 
                    Value = s.Key, 
                    Text = s.Value,
                    Selected = stationFilter == s.Key
                }).ToList()
            };
            filterViewModel.Filters.Add(stationFilterDef);
            
            var departmentFilterDef = new FilterDefinition
            {
                PropertyName = "Department", 
                DisplayName = "Department",
                Options = departmentNames.Select(d => new SelectListItem 
                { 
                    Value = d.Key, 
                    Text = d.Value,
                    Selected = departmentFilter == d.Key
                }).ToList()
            };
            filterViewModel.Filters.Add(departmentFilterDef);
            
            var distinctRoles = filteredEmployees.Where(e => !string.IsNullOrEmpty(e.Role))
                .Select(e => e.Role).Distinct().Take(50).ToList();
            
            var roleFilterDef = new FilterDefinition
            {
                PropertyName = "Role",
                DisplayName = "Role", 
                Options = distinctRoles.Select(r => new SelectListItem 
                { 
                    Value = r, 
                    Text = r,
                    Selected = roleFilter == r
                }).ToList()
            };
            filterViewModel.Filters.Add(roleFilterDef);
            
            ViewBag.Filters = filterViewModel;

            // Get total count and apply pagination
            var totalEmployees = filteredEmployees.Count();
            var employees = filteredEmployees
                .OrderBy(e => e.Fullname)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            // Map employees to view model
            var employeeViewModels = employees.Select(e => new EmployeeSearchResultViewModel
            {
                PayrollNo = e.PayrollNo,
                Name = e.Fullname ?? "Unknown",
                Designation = e.Designation ?? "N/A",
                Department = departmentNames.ContainsKey(e.Department ?? "") ? departmentNames[e.Department] : e.Department ?? "N/A",
                Role = e.Role ?? "N/A"
            }).ToList();

            // Create view model for the page
            var viewModel = new AddRoleGroupMemberViewModel
            {
                RoleGroupId = roleGroup.Id,
                RoleGroupName = roleGroup.Name
            };

            // Create pagination view model
            var filters = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(stationFilter)) filters["Station"] = stationFilter;
            if (!string.IsNullOrEmpty(departmentFilter)) filters["Department"] = departmentFilter;
            if (!string.IsNullOrEmpty(roleFilter)) filters["Role"] = roleFilter;
            
            ViewBag.Pagination = new PaginationViewModel
            {
                TotalItems = totalEmployees,
                ItemsPerPage = pageSize,
                CurrentPage = page,
                Action = "AddMember",
                Controller = "RoleGroups",
                RouteData = filters
            };

            ViewBag.InitialEmployees = employeeViewModels;
            ViewBag.SearchTerm = searchTerm;

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

        // GET: RoleGroups/SearchEmployees (Legacy - now handled by AddMember action)
        [HttpGet]
        public async Task<IActionResult> SearchEmployees(string stationCategory, string stationId, string departmentId, string role, string searchTerm)
        {
            // Get current user's payroll number from session
            var userPayrollNo = HttpContext.Session.GetString("EmployeePayrollNo");
            if (string.IsNullOrEmpty(userPayrollNo))
                return Json(new { results = new List<object>() });

            // Get visible departments and stations for current user
            var visibleDepartments = await _visibilityService.GetVisibleDepartmentsAsync(userPayrollNo);
            var visibleStations = await _visibilityService.GetVisibleStationsAsync(userPayrollNo);

            // Create base employee query with visibility filtering
            var employeesQuery = _ktdaContext.EmployeeBkps
                .Where(e => e.EmpisCurrActive == 0) // Active employees only
                .AsQueryable();

            // Apply visibility filtering
            if (visibleDepartments.Any())
            {
                var visibleDeptIds = visibleDepartments.Select(d => d.DepartmentId.ToString()).ToList();
                employeesQuery = employeesQuery.Where(e => visibleDeptIds.Contains(e.Department));
            }

            if (visibleStations.Any())
            {
                var visibleStationIds = visibleStations.Select(s => s.StationId.ToString()).ToList();
                // Also include HQ representation
                visibleStationIds.Add("HQ");
                visibleStationIds.Add("0");
                employeesQuery = employeesQuery.Where(e => visibleStationIds.Contains(e.Station));
            }

            // Create display name mappings
            var departmentNames = visibleDepartments.ToDictionary(d => d.DepartmentId.ToString(), d => d.DepartmentName);
            var stationNames = visibleStations.ToDictionary(s => s.StationId.ToString(), s => s.StationName);
            stationNames["HQ"] = "Head Office (HQ)";
            stationNames["0"] = "Head Office (HQ)";

            // Apply legacy filters for backward compatibility
            if (!string.IsNullOrEmpty(stationId))
            {
                employeesQuery = employeesQuery.Where(e => e.Station == stationId);
            }

            if (!string.IsNullOrEmpty(departmentId))
            {
                employeesQuery = employeesQuery.Where(e => e.Department == departmentId);
            }

            if (!string.IsNullOrEmpty(role))
            {
                employeesQuery = employeesQuery.Where(e => e.Role == role);
            }

            // Apply search term filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
                employeesQuery = employeesQuery.Where(e =>
                    (e.PayrollNo != null && e.PayrollNo.ToLower().Contains(searchTerm)) ||
                    (e.Fullname != null && e.Fullname.ToLower().Contains(searchTerm)) ||
                    (e.Role != null && e.Role.ToLower().Contains(searchTerm)) ||
                    (e.Designation != null && e.Designation.ToLower().Contains(searchTerm))
                );
            }

            // Get employees with limit for performance
            var employees = await employeesQuery
                .OrderBy(e => e.Fullname)
                .Take(50)
                .ToListAsync();
                
            // Map employees to view model
            var results = employees.Select(e => new
            {
                payrollNo = e.PayrollNo,
                name = e.Fullname ?? "Unknown",
                designation = e.Designation ?? "N/A",
                department = departmentNames.ContainsKey(e.Department ?? "") ? departmentNames[e.Department] : e.Department ?? "N/A",
                role = e.Role ?? "N/A"
            }).ToList();

            return Json(new { results });
        }

        private bool RoleGroupExists(int id)
        {
            return _context.RoleGroups.Any(e => e.Id == id);
        }
    }
}
