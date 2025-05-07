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
    public class RolesController : Controller
    {
        private readonly KtdaleaveContext _ktdaContext;
        private readonly IEmployeeService _employeeService;
        private readonly IDepartmentService _departmentService;
        private readonly ILogger<RolesController> _logger;

        public RolesController(
            KtdaleaveContext ktdaContext,
            IEmployeeService employeeService,
            IDepartmentService departmentService,
            ILogger<RolesController> logger)
        {
            _ktdaContext = ktdaContext;
            _employeeService = employeeService;
            _departmentService = departmentService;
            _logger = logger;
        }

        // GET: Roles
        public async Task<IActionResult> Index()
        {
            // Get distinct roles from EmployeeBkp table
            var roles = await _ktdaContext.EmployeeBkps
                .Where(e => !string.IsNullOrEmpty(e.Role))
                .Select(e => e.Role)
                .Distinct()
                .OrderBy(r => r)
                .ToListAsync();

            // Count employees per role
            var roleCounts = new Dictionary<string, int>();
            foreach (var role in roles)
            {
                var count = await _ktdaContext.EmployeeBkps
                    .CountAsync(e => e.Role == role);
                roleCounts[role] = count;
            }

            ViewBag.RoleCounts = roleCounts;
            return View(roles);
        }

        // GET: Roles/Details/{roleName}
        public async Task<IActionResult> Details(string id, int page = 1, string sortField = "Fullname", string sortOrder = "asc", string searchTerm = "")
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            // Set page size to 100
            int pageSize = 100;
            
            // Start with base query for employees with this role
            var employeesQuery = _ktdaContext.EmployeeBkps
                .Where(e => e.Role == id && e.EmpisCurrActive == 0);

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
                employeesQuery = employeesQuery.Where(e => 
                    e.Fullname.ToLower().Contains(searchTerm) || 
                    e.PayrollNo.ToLower().Contains(searchTerm) || 
                    e.Department.ToLower().Contains(searchTerm) || 
                    e.Station.ToLower().Contains(searchTerm) || 
                    e.Designation.ToLower().Contains(searchTerm));
            }
            
            // Get total count for pagination after filtering
            var totalEmployees = await employeesQuery.CountAsync();
                
            // Apply sorting
            employeesQuery = ApplySorting(employeesQuery, sortField, sortOrder);
            
            // Apply pagination
            var employees = await employeesQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (employees == null || !employees.Any())
            {
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    // If no results with search term, return empty list but don't show NotFound
                    employees = new List<EmployeeBkp>();
                }
                else
                {
                    return NotFound();
                }
            }

            // Create a dictionary to store department names
            var departmentNames = new Dictionary<string, string>();
            foreach (var employee in employees)
            {
                if (!string.IsNullOrEmpty(employee.Department) && !departmentNames.ContainsKey(employee.Department))
                {
                    try
                    {
                        int departmentId;
                        if (int.TryParse(employee.Department, out departmentId))
                        {
                            var department = await _departmentService.GetDepartmentByIdAsync(departmentId);
                            departmentNames[employee.Department] = department?.DepartmentName ?? "Unknown";
                        }
                        else
                        {
                            departmentNames[employee.Department] = "Invalid Department ID";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting department name for ID: {DepartmentId}", employee.Department);
                        departmentNames[employee.Department] = "Error";
                    }
                }
            }

            // Create a dictionary to store station names
            var stationNames = new Dictionary<string, string>();
            foreach (var employee in employees)
            {
                if (!string.IsNullOrEmpty(employee.Station) && !stationNames.ContainsKey(employee.Station))
                {
                    try
                    {
                        // Handle HQ special case
                        if (employee.Station.Equals("HQ", StringComparison.OrdinalIgnoreCase))
                        {
                            stationNames[employee.Station] = "HQ";
                        }
                        else
                        {
                            // Get all stations
                            var allStations = await _ktdaContext.Stations.ToListAsync();
                            
                            // Try to find a matching station by ID
                            // The employee.Station field contains the station ID formatted as a string (e.g., "001")
                            int stationId;
                            if (int.TryParse(employee.Station, out stationId))
                            {
                                // Find the station with this ID
                                var matchingStation = allStations.FirstOrDefault(s => s.StationId == stationId);
                                if (matchingStation != null && !string.IsNullOrEmpty(matchingStation.StationName))
                                {
                                    stationNames[employee.Station] = matchingStation.StationName;
                                    continue;
                                }
                            }
                            
                            // If we get here, we couldn't find a matching station
                            stationNames[employee.Station] = "Unknown Station";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting station name for ID: {StationId}", employee.Station);
                        stationNames[employee.Station] = "Error";
                    }
                }
            }

            // Set up pagination view model
            var paginationViewModel = new PaginationViewModel
            {
                CurrentPage = page,
                TotalItems = totalEmployees,
                ItemsPerPage = pageSize,
                Action = "Details",
                Controller = "Roles"
            };

            // Pass data to view
            ViewBag.RoleName = id;
            ViewBag.DepartmentNames = departmentNames;
            ViewBag.StationNames = stationNames;
            ViewBag.Pagination = paginationViewModel;
            ViewBag.TotalCount = totalEmployees;
            ViewBag.SortField = sortField;
            ViewBag.SortOrder = sortOrder;
            ViewBag.SearchTerm = searchTerm;

            return View(employees);
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
                default:
                    query = isAscending ? query.OrderBy(e => e.Fullname) : query.OrderByDescending(e => e.Fullname);
                    break;
            }
            
            return query;
        }
    }
}
