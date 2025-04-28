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
        private readonly IMediaService _mediaService;

        public MaterialsController(
            RequisitionContext context, 
            VendorService vendorService,
            IStationCategoryService stationCategoryService,
            IEmployeeService employeeService,
            KtdaleaveContext ktdacontext,
            IMediaService mediaService)
        {
            _context = context;
            _vendorService = vendorService;
            _stationCategoryService = stationCategoryService;
            _employeeService = employeeService;
            _ktdacontext = ktdacontext;
            _mediaService = mediaService;

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
                 .Include(m => m.MaterialAssignments.Where(ma => ma.IsActive))  // Include active assignments
                 .AsQueryable();

            // Create filter view model with explicit type for the array
            ViewBag.Filters = await query.CreateFiltersAsync(
                new Expression<Func<Material, object>>[] {
                    // Select which properties to create filters for
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
            
            // Add location filter based on MaterialAssignments
            var locationFilter = new FilterDefinition
            {
                PropertyName = "Location",
                DisplayName = "Current Location",
                Options = await _context.MaterialAssignments
                    .Where(ma => ma.IsActive)
                    .Select(ma => ma.Station)
                    .Distinct()
                    .OrderBy(s => s)
                    .Select(s => new SelectListItem
                    {
                        Value = s,
                        Text = s,
                        Selected = filters.ContainsKey("Location") && filters["Location"] == s
                    })
                    .ToListAsync()
            };
            ViewBag.Filters.Filters.Add(locationFilter);

            // Apply filters to query
            query = query.ApplyFilters(filters);
            
            // Apply location filter if present
            if (filters.TryGetValue("Location", out var location) && !string.IsNullOrEmpty(location))
            {
                query = query.Where(m => m.MaterialAssignments.Any(ma => ma.IsActive && ma.Station == location));
            }

            // Get total count for pagination
            var totalItems = await query.CountAsync();

            // Apply pagination and ordering
            var materials = await query
                .Include(m => m.MaterialCategory)
                .Include(m => m.MaterialAssignments.Where(ma => ma.IsActive))
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
                Controller = "Materials",
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
                .Include(m => m.MaterialSubcategory)
                .Include(m => m.MaterialAssignments.OrderByDescending(ma => ma.AssignmentDate))
                .Include(m => m.MaterialConditions.OrderByDescending(mc => mc.InspectionDate))
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (material == null)
            {
                return NotFound();
            }
            
            // Get all vendors in one batch
            var allVendors = await _vendorService.GetVendorsAsync();

            // Create a dictionary of vendor IDs to names
            var vendorNames = allVendors.ToDictionary(
                v => v.VendorID.ToString(),
                v => v.Name
            );

            // Pass the dictionary to the view
            ViewBag.VendorNames = vendorNames;

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
                    viewModel.Assignment.DepartmentId = departmentId;
                    
                    // Set the assignment's PayrollNo to the current user
                    viewModel.Assignment.PayrollNo = payrollNo;
                    viewModel.Assignment.AssignedByPayrollNo = payrollNo;
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
            viewModel.Departments = new SelectList(departments, "DepartmentId", "DepartmentName", viewModel.Assignment.DepartmentId);
            
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

        [HttpGet]
        public async Task<IActionResult> GetCategoryImage(int categoryId)
        {
            if (categoryId <= 0)
            {
                return Json("");
            }

            var categoryImage = await _mediaService.GetFirstMediaForModelAsync("MaterialCategory", categoryId);
            if (categoryImage != null)
            {
                return Json("/" + categoryImage.FilePath);
            }

            return Json("");
        }

        // POST: Materials/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateMaterialViewModel viewModel)
        {
            bool isAjaxRequest = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            try
            {
                // Set default values for required fields before validation
                if (string.IsNullOrEmpty(viewModel.Assignment?.PayrollNo))
                {
                    if (viewModel.Assignment == null)
                    {
                        viewModel.Assignment = new MaterialAssignment();
                    }
                    viewModel.Assignment.PayrollNo = "NotAssigned";
                    
                    // Clear any validation errors for this field
                    ModelState.Remove("Assignment.PayrollNo");
                }

                // Log the model state for debugging
                if (!ModelState.IsValid)
                {
                    // Log validation errors
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => new { 
                            Key = x.Key, 
                            Errors = x.Value.Errors.Select(e => e.ErrorMessage).ToList() 
                        })
                        .ToList();
                    
                    Console.WriteLine($"Model validation failed with {errors.Count} error(s):");
                    foreach (var error in errors)
                    {
                        Console.WriteLine($"- {error.Key}: {string.Join(", ", error.Errors)}");
                    }
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        // Generate a unique code if not provided
                        if (string.IsNullOrEmpty(viewModel.Material.Code))
                        {
                            // Format: MC-{CategoryId}-{Timestamp}
                            viewModel.Material.Code = $"MC-{viewModel.Material.MaterialCategoryId}-{DateTime.Now:yyyyMMddHHmmss}";
                        }

                        // Set creation timestamp
                        viewModel.Material.CreatedAt = DateTime.UtcNow;

                        // Save the material to the database
                        _context.Add(viewModel.Material);
                        await _context.SaveChangesAsync();

                        // Restore image handling
                        // Handle main image upload if provided
                        if (viewModel.ImageFile != null && viewModel.ImageFile.Length > 0)
                        {
                            await _mediaService.SaveMediaFileAsync(
                                viewModel.ImageFile,
                                "Material",
                                viewModel.Material.Id,
                                "main");
                        }

                        // Handle gallery images if provided
                        if (viewModel.GalleryFiles != null && viewModel.GalleryFiles.Any())
                        {
                            foreach (var galleryFile in viewModel.GalleryFiles)
                            {
                                if (galleryFile != null && galleryFile.Length > 0)
                                {
                                    await _mediaService.SaveMediaFileAsync(
                                        galleryFile,
                                        "Material",
                                        viewModel.Material.Id,
                                        "gallery");
                                }
                            }
                        }

                        // Create a new MaterialAssignment
                        var assignment = new MaterialAssignment
                        {
                            MaterialId = viewModel.Material.Id,
                            PayrollNo = string.IsNullOrEmpty(viewModel.Assignment?.PayrollNo) ? "NotAssigned" : viewModel.Assignment.PayrollNo,
                            AssignmentDate = DateTime.UtcNow,
                            StationCategory = viewModel.SelectedLocationCategory,
                            Station = viewModel.CurrentLocationId,
                            DepartmentId = viewModel.Assignment?.DepartmentId ?? 0,
                            AssignmentType = viewModel.Assignment?.AssignmentType ?? AssignmentType.New,
                            AssignedByPayrollNo = HttpContext.Session.GetString("EmployeePayrollNo") ?? "System",
                            IsActive = true,
                            Notes = viewModel.Assignment?.Notes ?? "Initial assignment at creation"
                        };

                        _context.MaterialAssignments.Add(assignment);
                        await _context.SaveChangesAsync(); // Save to get the new assignment ID
                        
                        // Create initial condition record
                        var condition = new MaterialCondition
                        {
                            MaterialId = viewModel.Material.Id,
                            MaterialAssignmentId = assignment.Id,
                            ConditionCheckType = ConditionCheckType.Initial,
                            Stage = "Creation",
                            Condition = viewModel.Material.Status,
                            FunctionalStatus = FunctionalStatus.FullyFunctional,
                            CosmeticStatus = CosmeticStatus.Excellent,
                            InspectedBy = HttpContext.Session.GetString("EmployeePayrollNo"),
                            InspectionDate = DateTime.UtcNow,
                            Notes = "Initial condition at creation"
                        };

                        _context.MaterialConditions.Add(condition);
                        await _context.SaveChangesAsync();

                        // Set success message
                        TempData["SuccessMessage"] = $"Material '{viewModel.Material.Name}' created successfully.";

                        // Return different responses based on request type
                        if (isAjaxRequest)
                        {
                            return Json(new { success = true, redirectUrl = Url.Action("Index") });
                        }

                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error creating material: {ex.Message}");
                        Console.WriteLine($"Stack trace: {ex.StackTrace}");
                        
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                        }
                        
                        ModelState.AddModelError("", $"Error creating material: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error in Create action: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                ModelState.AddModelError("", $"Unexpected error: {ex.Message}");
            }

            // If we got this far, something failed, redisplay form
            // Reload dropdown lists
            await ReloadFormData(viewModel);

            // Check if AJAX request
            if (isAjaxRequest)
            {
                // Return detailed validation errors for debugging
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value.Errors.Select(e => e.ErrorMessage).ToList()
                    );
                
                return BadRequest(errors);
            }

            return View(viewModel);
        }

        // Helper method to reload form data
        private async Task ReloadFormData(CreateMaterialViewModel viewModel)
        {
            viewModel.MaterialCategories = new SelectList(await _context.MaterialCategories.ToListAsync(), "Id", "Name", viewModel.Material.MaterialCategoryId);

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

            if (!string.IsNullOrEmpty(viewModel.SelectedLocationCategory))
            {
                viewModel.LocationOptions = await _stationCategoryService.GetLocationsForCategoryAsync(
                    viewModel.SelectedLocationCategory, viewModel.CurrentLocationId);
            }

            var departments = await _ktdacontext.Departments.ToListAsync();
            viewModel.Departments = new SelectList(departments, "DepartmentId", "DepartmentName", viewModel.Assignment.DepartmentId);
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
                .Include(m => m.MaterialAssignments.Where(ma => ma.IsActive))
                .Include(m => m.MaterialAssignments.OrderByDescending(ma => ma.AssignmentDate).Take(5))
                .Include(m => m.MaterialConditions.OrderByDescending(mc => mc.InspectionDate).Take(5))
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (material == null)
            {
                return NotFound();
            }
            
            var viewModel = new EditMaterialViewModel
            {
                Material = material,
                AssignmentHistory = material.MaterialAssignments?.OrderByDescending(ma => ma.AssignmentDate),
                ConditionHistory = material.MaterialConditions?.OrderByDescending(mc => mc.InspectionDate)
            };
            
            // Get the active assignment
            var activeAssignment = material.MaterialAssignments?.FirstOrDefault(ma => ma.IsActive);
            if (activeAssignment != null)
            {
                viewModel.Assignment = activeAssignment;
                viewModel.SelectedLocationCategory = activeAssignment.StationCategory;
                viewModel.CurrentLocationId = activeAssignment.Station;
            }
            
            // Initialize a new condition record
            viewModel.Condition = new MaterialCondition
            {
                MaterialId = material.Id,
                MaterialAssignmentId = activeAssignment?.Id,
                ConditionCheckType = ConditionCheckType.Periodic,
                Stage = "Update",
                Condition = material.Status,
                InspectionDate = DateTime.UtcNow
            };
            
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
                    viewModel.SelectedLocationCategory, viewModel.CurrentLocationId);
            }
            
            // Load departments
            var departments = await _ktdacontext.Departments.ToListAsync();
            viewModel.Departments = new SelectList(departments, "DepartmentId", "DepartmentName", activeAssignment?.DepartmentId);
            
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
                    // Get the existing material with its active assignment
                    var existingMaterial = await _context.Materials
                        .Include(m => m.MaterialAssignments.Where(ma => ma.IsActive))
                        .FirstOrDefaultAsync(m => m.Id == id);
                        
                    if (existingMaterial == null)
                    {
                        return NotFound();
                    }
                    
                    // Get the active assignment
                    var activeAssignment = existingMaterial.MaterialAssignments?.FirstOrDefault();
                    
                    // Update basic material properties
                    existingMaterial.Name = viewModel.Material.Name;
                    existingMaterial.Description = viewModel.Material.Description;
                    existingMaterial.MaterialCategoryId = viewModel.Material.MaterialCategoryId;
                    existingMaterial.MaterialSubcategoryId = viewModel.Material.MaterialSubcategoryId;
                    existingMaterial.Code = viewModel.Material.Code;
                    existingMaterial.VendorId = viewModel.Material.VendorId;
                    existingMaterial.Status = viewModel.Material.Status;
                    existingMaterial.UpdatedAt = DateTime.UtcNow;
                    
                    // Add new warranty and lifecycle properties
                    existingMaterial.PurchaseDate = viewModel.Material.PurchaseDate;
                    existingMaterial.PurchasePrice = viewModel.Material.PurchasePrice;
                    existingMaterial.WarrantyStartDate = viewModel.Material.WarrantyStartDate;
                    existingMaterial.WarrantyEndDate = viewModel.Material.WarrantyEndDate;
                    existingMaterial.WarrantyTerms = viewModel.Material.WarrantyTerms;
                    existingMaterial.ExpectedLifespanMonths = viewModel.Material.ExpectedLifespanMonths;
                    existingMaterial.MaintenanceIntervalMonths = viewModel.Material.MaintenanceIntervalMonths;
                    existingMaterial.LastMaintenanceDate = viewModel.Material.LastMaintenanceDate;
                    existingMaterial.NextMaintenanceDate = viewModel.Material.NextMaintenanceDate;
                    existingMaterial.Manufacturer = viewModel.Material.Manufacturer;
                    existingMaterial.ModelNumber = viewModel.Material.ModelNumber;
                    existingMaterial.SerialNumber = viewModel.Material.SerialNumber;
                    existingMaterial.AssetTag = viewModel.Material.AssetTag;
                    existingMaterial.Specifications = viewModel.Material.Specifications;
                    
                    // Update the material
                    _context.Update(existingMaterial);
                    
                    // Check if location has changed
                    bool locationChanged = activeAssignment == null ||
                                          activeAssignment.StationCategory != viewModel.SelectedLocationCategory ||
                                          activeAssignment.Station != viewModel.CurrentLocationId;
                                          
                    if (locationChanged)
                    {
                        // If there's an active assignment, close it
                        if (activeAssignment != null)
                        {
                            activeAssignment.IsActive = false;
                            activeAssignment.ReturnDate = DateTime.UtcNow;
                            _context.Update(activeAssignment);
                        }
                        
                        // Create a new assignment
                        var newAssignment = new MaterialAssignment
                        {
                            MaterialId = existingMaterial.Id,
                            PayrollNo = HttpContext.Session.GetString("EmployeePayrollNo") ?? "Unknown",
                            AssignmentDate = DateTime.UtcNow,
                            StationCategory = viewModel.SelectedLocationCategory,
                            Station = viewModel.CurrentLocationId,
                            DepartmentId = viewModel.Assignment.DepartmentId,
                            AssignmentType = AssignmentType.Transfer,
                            AssignedByPayrollNo = HttpContext.Session.GetString("EmployeePayrollNo") ?? "System",
                            IsActive = true,
                            Notes = "Updated location during edit"
                        };
                        
                        _context.MaterialAssignments.Add(newAssignment);
                        await _context.SaveChangesAsync(); // Save to get the new assignment ID
                        
                        // Create a condition record for the transfer
                        var transferCondition = new MaterialCondition
                        {
                            MaterialId = existingMaterial.Id,
                            MaterialAssignmentId = newAssignment.Id,
                            ConditionCheckType = ConditionCheckType.Assignment,
                            Stage = "Transfer",
                            Condition = existingMaterial.Status,
                            FunctionalStatus = viewModel.Condition.FunctionalStatus,
                            CosmeticStatus = viewModel.Condition.CosmeticStatus,
                            InspectedBy = HttpContext.Session.GetString("EmployeePayrollNo"),
                            InspectionDate = DateTime.UtcNow,
                            Notes = viewModel.Condition.Notes ?? "Location updated during edit"
                        };
                        
                        _context.MaterialConditions.Add(transferCondition);
                    }
                    else if (viewModel.Condition != null && 
                            (viewModel.Condition.FunctionalStatus.HasValue || 
                             viewModel.Condition.CosmeticStatus.HasValue || 
                             !string.IsNullOrEmpty(viewModel.Condition.Notes)))
                    {
                        // Create a condition record for the update if condition data was provided
                        var updateCondition = new MaterialCondition
                        {
                            MaterialId = existingMaterial.Id,
                            MaterialAssignmentId = activeAssignment?.Id,
                            ConditionCheckType = ConditionCheckType.Periodic,
                            Stage = "Update",
                            Condition = existingMaterial.Status,
                            FunctionalStatus = viewModel.Condition.FunctionalStatus,
                            CosmeticStatus = viewModel.Condition.CosmeticStatus,
                            InspectedBy = HttpContext.Session.GetString("EmployeePayrollNo"),
                            InspectionDate = DateTime.UtcNow,
                            Notes = viewModel.Condition.Notes ?? "Condition updated during edit"
                        };
                        
                        _context.MaterialConditions.Add(updateCondition);
                    }
                    
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Material updated successfully.";
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
            }
            
            // If we got this far, something failed, redisplay form
            // Reload dropdown lists
            viewModel.MaterialCategories = new SelectList(await _context.MaterialCategories.ToListAsync(), "Id", "Name", viewModel.Material.MaterialCategoryId);
            
            if (viewModel.Material.MaterialCategoryId > 0)
            {
                viewModel.MaterialSubcategories = new SelectList(
                    await _context.MaterialSubCategories
                        .Where(sc => sc.MaterialCategoryId == viewModel.Material.MaterialCategoryId)
                        .OrderBy(sc => sc.Name)
                        .ToListAsync(),
                    "Id", "Name", viewModel.Material.MaterialSubcategoryId);
            }
            
            viewModel.Vendors = new SelectList(await _vendorService.GetVendorsAsync(), "VendorID", "Name", viewModel.Material.VendorId);
            viewModel.StationCategories = await _stationCategoryService.GetStationCategoriesSelectListAsync("both");
            
            if (!string.IsNullOrEmpty(viewModel.SelectedLocationCategory))
            {
                viewModel.LocationOptions = await _stationCategoryService.GetLocationsForCategoryAsync(
                    viewModel.SelectedLocationCategory, viewModel.CurrentLocationId);
            }
            
            var departments = await _ktdacontext.Departments.ToListAsync();
            viewModel.Departments = new SelectList(departments, "DepartmentId", "DepartmentName", viewModel.Assignment.DepartmentId);
            
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
