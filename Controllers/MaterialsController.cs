using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MRIV.Models;
using MRIV.Services;
using MRIV.Enums;
using MRIV.ViewModels;
using MRIV.Attributes;
using MRIV.Extensions;
using System.Linq.Expressions;

namespace MRIV.Controllers
{
    [CustomAuthorize]
    public class MaterialsController : Controller
    {
        private readonly RequisitionContext _context;
        private readonly VendorService _vendorService;
        private readonly IStationCategoryService _stationCategoryService;
        private readonly IEmployeeService _employeeService;

        public MaterialsController(
            RequisitionContext context, 
            VendorService vendorService,
            IStationCategoryService stationCategoryService,
            IEmployeeService employeeService)
        {
            _context = context;
            _vendorService = vendorService;
            _stationCategoryService = stationCategoryService;
            _employeeService = employeeService;
        }

        // GET: Materials
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            // Ensure valid pagination parameters
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : (pageSize > 100 ? 100 : pageSize);

            // Get filter values from request query string
            var filters = new Dictionary<string, string>();
            foreach (var key in Request.Query.Keys.Where(k => k != "page" && k != "pageSize"))
            {
                filters[key] = Request.Query[key];
            }

            // Create base query
            var query = _context.Materials.AsQueryable();

            // Create filter view model with explicit type for the array
            ViewBag.Filters = await query.CreateFiltersAsync(
                new Expression<Func<Material, object>>[] {
            // Select which properties to create filters for
            m => m.MaterialCategory,
            m => m.CurrentLocationId,
            m => m.Status,
                    // Add other properties as needed
                },
                filters
            );

            // Apply filters to query
            query = query.ApplyFilters(filters);

            // Get total count for pagination
            var totalItems = await query.CountAsync();

            // Apply pagination and ordering
            var materials = await query
                .Include(m => m.MaterialCategory)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

           
            // Get all vendors in one batch
            var allVendors = await _vendorService.GetVendorsAsync();

            // Create a dictionary of vendor IDs to names
            var vendorNames = allVendors.ToDictionary(
                v => v.VendorID.ToString(),
                v => v.Name
            );

            // Pass the dictionary to the view
            ViewBag.VendorNames = vendorNames;
            // Create pagination view model
            var paginationModel = new PaginationViewModel
            {
                TotalItems = totalItems,
                ItemsPerPage = pageSize,
                CurrentPage = page,
                Action = "Index",
                Controller = "Requisitions",
                RouteData = filters
            };

            // Pass pagination model to view
            ViewBag.Pagination = paginationModel;

            return View(materials);
        }

        // GET: Materials/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var material = await _context.Materials
                .Include(m => m.MaterialCategory)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (material == null)
            {
                return NotFound();
            }

            return View(material);
        }

        // GET: Materials/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new CreateMaterialViewModel();
            
            // Get the user's current location/station for context
            var payrollNo = HttpContext.Session.GetString("EmployeePayrollNo");
            if (!string.IsNullOrEmpty(payrollNo))
            {
                var (employee, department, userStation) = await _employeeService.GetEmployeeAndDepartmentAsync(payrollNo);
            }
            
            // Load material categories
            viewModel.MaterialCategories = new SelectList(await _context.MaterialCategories.ToListAsync(), "Id", "Name");
            
            // Load vendors
            viewModel.Vendors = new SelectList(await _vendorService.GetVendorsAsync(), "VendorID", "Name");
            
            // Load station categories for location selection
            viewModel.StationCategories = await _stationCategoryService.GetStationCategoriesSelectListAsync("both");
            
            // Set default status
            viewModel.Material.Status = MaterialStatus.GoodCondition;
            
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult GetNextCode(int categoryId, string rowIndex)
        {
            // Handle rowIndex conversion
            int index = 0;
            if (!string.IsNullOrEmpty(rowIndex) && int.TryParse(rowIndex, out int parsedIndex))
            {
                index = parsedIndex;
            }

            int nextId = _context.Materials.Count() + 1;
            string baseCode = $"MAT-{nextId:D3}-{categoryId:D3}";

            if (index > 0)
            {
                baseCode += $"-{index}";
            }
            // Add a unique component
            string uniqueCode = $"{baseCode}-{DateTime.Now.Millisecond:D3}";

            return Json(uniqueCode);
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

        // POST: Materials/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateMaterialViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Generate a unique code if not provided
                    if (string.IsNullOrEmpty(viewModel.Material.Code))
                    {
                        // Format: MC-{CategoryId}-{Timestamp}
                        viewModel.Material.Code = $"MC-{viewModel.Material.MaterialCategoryId}-{DateTime.Now.ToString("yyyyMMddHHmmss")}";
                    }
                    
                    // Save the material to the database
                    _context.Add(viewModel.Material);
                    await _context.SaveChangesAsync();
                    
                    // Set success message
                    TempData["SuccessMessage"] = $"Material '{viewModel.Material.Name}' created successfully.";
                    
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating material: {ex.Message}");
                }
            }
            
            // If we got this far, something failed, redisplay form
            // Reload dropdown lists
            viewModel.MaterialCategories = new SelectList(await _context.MaterialCategories.ToListAsync(), "Id", "Name", viewModel.Material.MaterialCategoryId);
            viewModel.Vendors = new SelectList(await _vendorService.GetVendorsAsync(), "VendorID", "Name", viewModel.Material.VendorId);
            viewModel.StationCategories = await _stationCategoryService.GetStationCategoriesSelectListAsync("both");
            
            if (!string.IsNullOrEmpty(viewModel.SelectedLocationCategory))
            {
                viewModel.LocationOptions = await _stationCategoryService.GetLocationsForCategoryAsync(
                    viewModel.SelectedLocationCategory, viewModel.Material.CurrentLocationId);
            }
            
            return View(viewModel);
        }

        // GET: Materials/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var material = await _context.Materials.FindAsync(id);
            if (material == null)
            {
                return NotFound();
            }
            ViewData["MaterialCategoryId"] = new SelectList(_context.MaterialCategories, "Id", "Name", material.MaterialCategoryId);
            return View(material);
        }

        // POST: Materials/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,MaterialCategoryId,Code,Name,Description,CurrentLocationId,VendorId,Status")] Material material)
        {
            if (id != material.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(material);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MaterialExists(material.Id))
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
            ViewData["MaterialCategoryId"] = new SelectList(_context.MaterialCategories, "Id", "Name", material.MaterialCategoryId);
            return View(material);
        }

        // GET: Materials/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var material = await _context.Materials
                .Include(m => m.MaterialCategory)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (material == null)
            {
                return NotFound();
            }

            return View(material);
        }

        // POST: Materials/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var material = await _context.Materials.FindAsync(id);
            if (material != null)
            {
                _context.Materials.Remove(material);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MaterialExists(int id)
        {
            return _context.Materials.Any(e => e.Id == id);
        }
    }
}
