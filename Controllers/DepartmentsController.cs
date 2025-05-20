using Microsoft.AspNetCore.Mvc;
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
    public class DepartmentsController : Controller
    {
        private readonly KtdaleaveContext _ktdaContext;
        private readonly IEmployeeService _employeeService;
        private readonly IDepartmentService _departmentService;
        private readonly ILogger<DepartmentsController> _logger;

        public DepartmentsController(
            KtdaleaveContext ktdaContext,
            IEmployeeService employeeService,
            IDepartmentService departmentService,
            ILogger<DepartmentsController> logger)
        {
            _ktdaContext = ktdaContext;
            _employeeService = employeeService;
            _departmentService = departmentService;
            _logger = logger;
        }

        // GET: Departments
        public async Task<IActionResult> Index(int page = 1, string sortField = "DepartmentName", string sortOrder = "asc", string searchTerm = "")
        {
            // Set page size
            int pageSize = 10;
            
            // Start with base query for departments
            var departmentsQuery = _ktdaContext.Departments.AsQueryable();

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
                departmentsQuery = departmentsQuery.Where(d => 
                    d.DepartmentName.ToLower().Contains(searchTerm) || 
                    (d.DepartmentId != null && d.DepartmentId.ToLower().Contains(searchTerm)) ||
                    (d.DepartmentHd != null && d.DepartmentHd.ToLower().Contains(searchTerm)) ||
                    (d.Emailaddress != null && d.Emailaddress.ToLower().Contains(searchTerm)));
            }
            
            // Get total count for pagination after filtering
            var totalDepartments = await departmentsQuery.CountAsync();
                
            // Apply sorting
            departmentsQuery = ApplySorting(departmentsQuery, sortField, sortOrder);
            
            // Apply pagination
            var departments = await departmentsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Create dictionaries to store department head names and designations
            var departmentHeadNames = new Dictionary<string, string>();
            var departmentHeadDesignations = new Dictionary<string, string>();
            
            foreach (var department in departments)
            {
                if (!string.IsNullOrEmpty(department.DepartmentHd) && !departmentHeadNames.ContainsKey(department.DepartmentHd))
                {
                    try
                    {
                        var employee = await _employeeService.GetEmployeeByPayrollAsync(department.DepartmentHd);
                        departmentHeadNames[department.DepartmentHd] = employee?.Fullname ?? "Unknown";
                        departmentHeadDesignations[department.DepartmentHd] = employee?.Designation ?? "";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting employee name for payroll: {PayrollNo}", department.DepartmentHd);
                        departmentHeadNames[department.DepartmentHd] = "Error";
                        departmentHeadDesignations[department.DepartmentHd] = "";
                    }
                }
            }

            // Set up pagination view model
            var paginationViewModel = new PaginationViewModel
            {
                CurrentPage = page,
                TotalItems = totalDepartments,
                ItemsPerPage = pageSize,
                Action = "Index",
                Controller = "Departments"
            };

            // Pass data to view
            ViewBag.DepartmentHeadNames = departmentHeadNames;
            ViewBag.DepartmentHeadDesignations = departmentHeadDesignations;
            ViewBag.Pagination = paginationViewModel;
            ViewBag.TotalCount = totalDepartments;
            ViewBag.SortField = sortField;
            ViewBag.SortOrder = sortOrder;
            ViewBag.SearchTerm = searchTerm;

            return View(departments);
        }

        // GET: Departments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _ktdaContext.Departments
                .FirstOrDefaultAsync(m => m.DepartmentCode == id);
                
            if (department == null)
            {
                return NotFound();
            }

            // Get department head name if available
            if (!string.IsNullOrEmpty(department.DepartmentHd))
            {
                try
                {
                    var employee = await _employeeService.GetEmployeeByPayrollAsync(department.DepartmentHd);
                    ViewBag.DepartmentHeadName = employee?.Fullname ?? "Unknown";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting employee name for payroll: {PayrollNo}", department.DepartmentHd);
                    ViewBag.DepartmentHeadName = "Error retrieving name";
                }
            }
            else
            {
                ViewBag.DepartmentHeadName = "Not assigned";
            }

            return View(department);
        }

        // GET: Departments/Create
        public async Task<IActionResult> Create()
        {
            // Get employees for department head selection - for Create we'll show all active employees
            var employees = await _ktdaContext.EmployeeBkps
                .Where(e => e.EmpisCurrActive == 0)
                .OrderBy(e => e.Fullname)
                .ToListAsync();
                
            ViewBag.Employees = employees;
            return View();
        }

        // POST: Departments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DepartmentCode,DepartmentId,DepartmentName,DepartmentHd,Emailaddress,UserName,OrgCode")] Department department)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _ktdaContext.Add(department);
                    await _ktdaContext.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating department");
                    ModelState.AddModelError("", "An error occurred while creating the department. Please try again.");
                }
            }
            
            // If we got this far, something failed, redisplay form
            var employees = await _ktdaContext.EmployeeBkps
                .Where(e => e.EmpisCurrActive == 0)
                .OrderBy(e => e.Fullname)
                .ToListAsync();
                
            ViewBag.Employees = employees;
            return View(department);
        }

        // GET: Departments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _ktdaContext.Departments.FindAsync(id);
            if (department == null)
            {
                return NotFound();
            }
            
            // Get employees for department head selection
            var employees = await _ktdaContext.EmployeeBkps
                .Where(e => e.Department == department.DepartmentId)
                .Where(e => e.EmpisCurrActive == 0)
                .OrderBy(e => e.Fullname)
                .ToListAsync();
                
            ViewBag.Employees = employees;
            return View(department);
        }

        // POST: Departments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DepartmentCode,DepartmentId,DepartmentName,DepartmentHd,Emailaddress,UserName,OrgCode")] Department department)
        {
            if (id != department.DepartmentCode)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _ktdaContext.Update(department);
                    await _ktdaContext.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepartmentExists(department.DepartmentCode))
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
                    _logger.LogError(ex, "Error updating department");
                    ModelState.AddModelError("", "An error occurred while updating the department. Please try again.");
                }
            }
            
            // If we got this far, something failed, redisplay form
            var employees = await _ktdaContext.EmployeeBkps
                .Where(e => e.EmpisCurrActive == 0)
                .OrderBy(e => e.Fullname)
                .ToListAsync();
                
            ViewBag.Employees = employees;
            return View(department);
        }

        // GET: Departments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _ktdaContext.Departments
                .FirstOrDefaultAsync(m => m.DepartmentCode == id);
                
            if (department == null)
            {
                return NotFound();
            }

            // Get department head name if available
            if (!string.IsNullOrEmpty(department.DepartmentHd))
            {
                try
                {
                    var employee = await _employeeService.GetEmployeeByPayrollAsync(department.DepartmentHd);
                    ViewBag.DepartmentHeadName = employee?.Fullname ?? "Unknown";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting employee name for payroll: {PayrollNo}", department.DepartmentHd);
                    ViewBag.DepartmentHeadName = "Error retrieving name";
                }
            }
            else
            {
                ViewBag.DepartmentHeadName = "Not assigned";
            }

            return View(department);
        }

        // POST: Departments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var department = await _ktdaContext.Departments.FindAsync(id);
            if (department != null)
            {
                try
                {
                    _ktdaContext.Departments.Remove(department);
                    await _ktdaContext.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting department with ID {DepartmentId}", id);
                    ModelState.AddModelError("", "An error occurred while deleting the department. It may be referenced by other records.");
                    
                    // Get department head name if available
                    if (!string.IsNullOrEmpty(department.DepartmentHd))
                    {
                        try
                        {
                            var employee = await _employeeService.GetEmployeeByPayrollAsync(department.DepartmentHd);
                            ViewBag.DepartmentHeadName = employee?.Fullname ?? "Unknown";
                        }
                        catch
                        {
                            ViewBag.DepartmentHeadName = "Error retrieving name";
                        }
                    }
                    else
                    {
                        ViewBag.DepartmentHeadName = "Not assigned";
                    }
                    
                    return View(department);
                }
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool DepartmentExists(int id)
        {
            return _ktdaContext.Departments.Any(e => e.DepartmentCode == id);
        }
        
        // Helper method to apply sorting to the query
        private IQueryable<Department> ApplySorting(IQueryable<Department> query, string sortField, string sortOrder)
        {
            // Default to sorting by DepartmentName if sortField is invalid
            if (string.IsNullOrEmpty(sortField))
            {
                sortField = "DepartmentName";
            }
            
            // Determine if sorting ascending or descending
            bool isAscending = string.IsNullOrEmpty(sortOrder) || sortOrder.ToLower() == "asc";
            
            // Apply sorting based on the field
            switch (sortField.ToLower())
            {
                case "departmentcode":
                    query = isAscending ? query.OrderBy(d => d.DepartmentCode) : query.OrderByDescending(d => d.DepartmentCode);
                    break;
                case "departmentid":
                    query = isAscending ? query.OrderBy(d => d.DepartmentId) : query.OrderByDescending(d => d.DepartmentId);
                    break;
                case "departmentname":
                    query = isAscending ? query.OrderBy(d => d.DepartmentName) : query.OrderByDescending(d => d.DepartmentName);
                    break;
                case "departmenthd":
                    query = isAscending ? query.OrderBy(d => d.DepartmentHd) : query.OrderByDescending(d => d.DepartmentHd);
                    break;
                case "emailaddress":
                    query = isAscending ? query.OrderBy(d => d.Emailaddress) : query.OrderByDescending(d => d.Emailaddress);
                    break;
                default:
                    query = isAscending ? query.OrderBy(d => d.DepartmentName) : query.OrderByDescending(d => d.DepartmentName);
                    break;
            }
            
            return query;
        }
    }
}
