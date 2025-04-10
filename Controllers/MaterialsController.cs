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
        private readonly KtdaleaveContext _ktdacontext;
        private readonly VendorService _vendorService;
        private readonly IStationCategoryService _stationCategoryService;
        private readonly IEmployeeService _employeeService;

        public MaterialsController(
            RequisitionContext context, 
            VendorService vendorService,
            IStationCategoryService stationCategoryService,
            IEmployeeService employeeService,
            KtdaleaveContext ktdacontext)
        {
            _context = context;
            _vendorService = vendorService;
            _stationCategoryService = stationCategoryService;
            _employeeService = employeeService;
            _ktdacontext = ktdacontext;

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
            var query = _context.Materials
                 .Include(m => m.MaterialCategory)  // Ensure MaterialCategory is loaded
                 .AsQueryable();

            // Create filter view model with explicit type for the array
            ViewBag.Filters = await query.CreateFiltersAsync(
                new Expression<Func<Material, object>>[] {
            // Select which properties to create filters for
            m => m.CurrentLocationId,
            m => m.Status,
                    // Add other properties as needed
                },
                filters
            );
            // Add custom MaterialCategory filter
            var categoryFilter = new FilterDefinition
            {
                PropertyName = "MaterialCategoryId",
                DisplayName = "Material Category",
                Options = await _context.MaterialCategories
                    .OrderBy(c => c.Name)
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name,
                        Selected = filters.ContainsKey("MaterialCategoryId") &&
                                  filters["MaterialCategoryId"] == c.Id.ToString()
                    })
                    .ToListAsync()
            };
            ViewBag.Filters.Filters.Insert(0, categoryFilter);

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
            Department loggedInUserDepartment = null;
            Station loggedInUserStation = null;

            if (!string.IsNullOrEmpty(payrollNo))
            {
                var (employee, department, userStation) = await _employeeService.GetEmployeeAndDepartmentAsync(payrollNo);
                loggedInUserDepartment = department;
                loggedInUserStation = userStation;

                // Set the department ID from the logged-in user
                if (department != null)
                {
                    var departmentId = Convert.ToInt32(department.DepartmentId);
                    viewModel.Material.DepartmentId = departmentId;
                   
                }
            }
            
            // Load material categories
            viewModel.MaterialCategories = new SelectList(await _context.MaterialCategories.ToListAsync(), "Id", "Name");
            
            // Load material subcategories (empty initially, will be populated via AJAX)
            viewModel.MaterialSubcategories = new SelectList(Enumerable.Empty<SelectListItem>());
            
            // Load vendors
            viewModel.Vendors = new SelectList(await _vendorService.GetVendorsAsync(), "VendorID", "Name");
            
            // Load station categories for location selection
            viewModel.StationCategories = await _stationCategoryService.GetStationCategoriesSelectListAsync("both");
            
            // Load all departments and set the logged-in user's department as the default
            var departments = await _ktdacontext.Departments.ToListAsync();
            viewModel.Departments = new SelectList(departments, "DepartmentId", "DepartmentName", viewModel.Material.DepartmentId);
            
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
        public async Task<IActionResult> GetSubcategoriesForCategory(int categoryId)
        {
            if (categoryId <= 0)
            {
                return Json(new List<object>());
            }

            var subcategories = await _context.MaterialSubCategories
                .Where(sc => sc.MaterialCategoryId == categoryId)
                .OrderBy(sc => sc.Name)
                .Select(sc => new { value = sc.Id, text = sc.Name })
                .ToListAsync();

            return Json(subcategories);
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
                    
                    // Set the StationCategory from the selected location category
                    viewModel.Material.StationCategory = viewModel.SelectedLocationCategory;
                    
                    // Set the Station based on the selected location
                    if (!string.IsNullOrEmpty(viewModel.Material.CurrentLocationId))
                    {
                        // If the station category is "headoffice", append "HQ" to the station
                        if (viewModel.SelectedLocationCategory?.ToLower() == "headoffice")
                        {
                            viewModel.Material.Station = $"HQ-{viewModel.Material.CurrentLocationId}";
                        }
                        else
                        {
                            viewModel.Material.Station = viewModel.Material.CurrentLocationId;
                        }
                    }
                    
                    // If department ID is not set, get it from the logged-in user
                    if (!viewModel.Material.DepartmentId.HasValue)
                    {
                        var payrollNo = HttpContext.Session.GetString("EmployeePayrollNo");
                        if (!string.IsNullOrEmpty(payrollNo))
                        {
                            var (employee, department, _) = await _employeeService.GetEmployeeAndDepartmentAsync(payrollNo);
                            if (department != null)
                            {
                                var departmentId = Convert.ToInt32(department.DepartmentId);
                                viewModel.Material.DepartmentId = departmentId;
                            }
                        }
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
            
            // Load material subcategories for the selected category
            if (viewModel.Material.MaterialCategoryId > 0)
            {
                viewModel.MaterialSubcategories = new SelectList(
                    await _context.MaterialSubCategories
                        .Where(sc => sc.MaterialCategoryId == viewModel.Material.MaterialCategoryId)
                        .OrderBy(sc => sc.Name)
                        .ToListAsync(),
                    "Id", "Name", viewModel.Material.MaterialSubcategoryId);
            }
            else
            {
                viewModel.MaterialSubcategories = new SelectList(Enumerable.Empty<SelectListItem>());
            }
            
            viewModel.Vendors = new SelectList(await _vendorService.GetVendorsAsync(), "VendorID", "Name", viewModel.Material.VendorId);
            viewModel.StationCategories = await _stationCategoryService.GetStationCategoriesSelectListAsync("both");
            
            // Reload departments
            var departments = await _ktdacontext.Departments.ToListAsync();
            viewModel.Departments = new SelectList(departments, "DepartmentId", "DepartmentName", viewModel.Material.DepartmentId);
            
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

            var material = await _context.Materials
                .Include(m => m.MaterialCategory)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (material == null)
            {
                return NotFound();
            }
            
            var viewModel = new EditMaterialViewModel
            {
                Material = material
            };
            
            // Set the selected location category based on the material's StationCategory
            viewModel.SelectedLocationCategory = material.StationCategory;
            
            // Load material categories
            viewModel.MaterialCategories = new SelectList(await _context.MaterialCategories.ToListAsync(), "Id", "Name", material.MaterialCategoryId);
            
            // Load material subcategories for the selected category
            if (material.MaterialCategoryId > 0)
            {
                viewModel.MaterialSubcategories = new SelectList(
                    await _context.MaterialSubCategories
                        .Where(sc => sc.MaterialCategoryId == material.MaterialCategoryId)
                        .OrderBy(sc => sc.Name)
                        .ToListAsync(),
                    "Id", "Name", material.MaterialSubcategoryId);
            }
            else
            {
                viewModel.MaterialSubcategories = new SelectList(Enumerable.Empty<SelectListItem>());
            }
            
            // Load vendors
            viewModel.Vendors = new SelectList(await _vendorService.GetVendorsAsync(), "VendorID", "Name", material.VendorId);
            
            // Load station categories for location selection
            viewModel.StationCategories = await _stationCategoryService.GetStationCategoriesSelectListAsync("both");
            
            // Load location options if a station category is selected
            if (!string.IsNullOrEmpty(viewModel.SelectedLocationCategory))
            {
                viewModel.LocationOptions = await _stationCategoryService.GetLocationsForCategoryAsync(
                    viewModel.SelectedLocationCategory, material.CurrentLocationId);
            }
            
            // Load all departments
            var departments = await _ktdacontext.Departments.ToListAsync();
            viewModel.Departments = new SelectList(departments, "DepartmentId", "DepartmentName", material.DepartmentId);
            
            return View(viewModel);
        }

        // POST: Materials/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditMaterialViewModel viewModel)
        {
            if (id != viewModel.Material.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Set the StationCategory from the selected location category
                    viewModel.Material.StationCategory = viewModel.SelectedLocationCategory;
                    
                    // Set the Station based on the selected location
                    if (!string.IsNullOrEmpty(viewModel.Material.CurrentLocationId))
                    {
                        // If the station category is "headoffice", append "HQ" to the station
                        if (viewModel.SelectedLocationCategory?.ToLower() == "headoffice")
                        {
                            viewModel.Material.Station = $"HQ-{viewModel.Material.CurrentLocationId}";
                        }
                        else
                        {
                            viewModel.Material.Station = viewModel.Material.CurrentLocationId;
                        }
                    }
                    
                    // Update the material in the database
                    viewModel.Material.UpdatedAt = DateTime.UtcNow;
                    _context.Update(viewModel.Material);
                    await _context.SaveChangesAsync();
                    
                    // Set success message
                    TempData["SuccessMessage"] = $"Material '{viewModel.Material.Name}' updated successfully.";
                    
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MaterialExists(viewModel.Material.Id))
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
                    ModelState.AddModelError("", $"Error updating material: {ex.Message}");
                }
            }
            
            // If we got this far, something failed, redisplay form
            // Reload dropdown lists
            viewModel.MaterialCategories = new SelectList(await _context.MaterialCategories.ToListAsync(), "Id", "Name", viewModel.Material.MaterialCategoryId);
            
            // Load material subcategories for the selected category
            if (viewModel.Material.MaterialCategoryId > 0)
            {
                viewModel.MaterialSubcategories = new SelectList(
                    await _context.MaterialSubCategories
                        .Where(sc => sc.MaterialCategoryId == viewModel.Material.MaterialCategoryId)
                        .OrderBy(sc => sc.Name)
                        .ToListAsync(),
                    "Id", "Name", viewModel.Material.MaterialSubcategoryId);
            }
            else
            {
                viewModel.MaterialSubcategories = new SelectList(Enumerable.Empty<SelectListItem>());
            }
            
            viewModel.Vendors = new SelectList(await _vendorService.GetVendorsAsync(), "VendorID", "Name", viewModel.Material.VendorId);
            viewModel.StationCategories = await _stationCategoryService.GetStationCategoriesSelectListAsync("both");
            
            // Reload departments
            var departments = await _ktdacontext.Departments.ToListAsync();
            viewModel.Departments = new SelectList(departments, "DepartmentId", "DepartmentName", viewModel.Material.DepartmentId);
            
            if (!string.IsNullOrEmpty(viewModel.SelectedLocationCategory))
            {
                viewModel.LocationOptions = await _stationCategoryService.GetLocationsForCategoryAsync(
                    viewModel.SelectedLocationCategory, viewModel.Material.CurrentLocationId);
            }
            
            return View(viewModel);
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
