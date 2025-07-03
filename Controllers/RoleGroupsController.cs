using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MRIV.Attributes;
using MRIV.Models;
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
        private readonly ILogger<RoleGroupsController> _logger;

        public RoleGroupsController(
            RequisitionContext context,
            KtdaleaveContext ktdaContext,
            Services.IEmployeeService employeeService,
            ILogger<RoleGroupsController> logger)
        {
            _context = context;
            _ktdaContext = ktdaContext;
            _employeeService = employeeService;
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

            return View("~/Views/Roles/AddMember.cshtml", viewModel);
        }

        // POST: RoleGroups/AddMember
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(AddRoleGroupMemberViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if employee exists
                var employee = await _employeeService.GetEmployeeByPayrollAsync(model.PayrollNo);
                if (employee == null)
                {
                    ModelState.AddModelError("PayrollNo", "Employee not found with this payroll number.");
                    return View("~/Views/Roles/AddMember.cshtml", model);
                }

                // Check if employee is already a member of this role group
                var existingMember = await _context.RoleGroupMembers
                    .FirstOrDefaultAsync(m => m.RoleGroupId == model.RoleGroupId && 
                                             m.PayrollNo == model.PayrollNo && 
                                             m.IsActive);
                
                if (existingMember != null)
                {
                    ModelState.AddModelError("PayrollNo", "This employee is already a member of this role group.");
                    return View("~/Views/Roles/AddMember.cshtml", model);
                }

                // Add the employee to the role group
                var roleGroupMember = new RoleGroupMember
                {
                    RoleGroupId = model.RoleGroupId,
                    PayrollNo = model.PayrollNo,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsActive = true
                };

                _context.Add(roleGroupMember);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Details), new { id = model.RoleGroupId });
            }

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

        private bool RoleGroupExists(int id)
        {
            return _context.RoleGroups.Any(e => e.Id == id);
        }
    }
}
