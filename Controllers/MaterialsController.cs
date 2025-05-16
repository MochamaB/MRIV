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
        private readonly ILocationService _locationService;

        public MaterialsController(
            RequisitionContext context,
            VendorService vendorService,
            IStationCategoryService stationCategoryService,
            IEmployeeService employeeService,
            KtdaleaveContext ktdacontext,
            IMediaService mediaService,
            ILocationService locationService)
        {
            _context = context;
            _vendorService = vendorService;
            _stationCategoryService = stationCategoryService;
            _employeeService = employeeService;
            _ktdacontext = ktdacontext;
            _mediaService = mediaService;
            _locationService = locationService;

        }

        // GET: Materials
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            // Pagination validation
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            // Get filters from query string
            var filters = Request.Query
                .Where(k => k.Key != "page" && k.Key != "pageSize")
                .ToDictionary(k => k.Key, v => v.Value.ToString());

            // Base query
            var query = _context.Materials
                .Include(m => m.MaterialCategory)
                 .Include(m => m.MaterialSubcategory)
                .Include(m => m.MaterialAssignments.Where(ma => ma.IsActive))
                    .ThenInclude(ma => ma.MaterialConditions.OrderByDescending(mc => mc.InspectionDate))
                .AsQueryable();

            // Apply filters
            query = query.ApplyFilters(filters);
            var totalItems = await query.CountAsync();

            // Get paginated materials
            var materials = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Initialize view model
            var viewModel = new MaterialsIndexViewModel
            {
                Materials = materials,
                Pagination = new PaginationViewModel
                {
                    TotalItems = totalItems,
                    ItemsPerPage = pageSize,
                    CurrentPage = page,
                    Action = "Index",
                    Controller = "Materials",
                    RouteData = filters
                },
                Filters = await query.CreateFiltersAsync(
                    new Expression<Func<Material, object>>[] { m => m.Status },
                    filters
                )
            };

            // Add category filter
            viewModel.Filters.Filters.Insert(0, new FilterDefinition
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
            });
            // Add subcategory filter
            viewModel.Filters.Filters.Insert(1, new FilterDefinition
            {
                PropertyName = "MaterialSubcategoryId",
                DisplayName = "Material Subcategory",
                Options = await _context.MaterialSubCategories
                    .Where(s => !filters.ContainsKey("MaterialCategoryId") ||
                           s.MaterialCategoryId.ToString() == filters["MaterialCategoryId"])
                    .OrderBy(c => c.Name)
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name,
                        Selected = filters.ContainsKey("MaterialSubcategoryId") &&
                                  filters["MaterialSubcategoryId"] == c.Id.ToString()
                    })
                    .ToListAsync()
            });

            // Process assignments and related data
            foreach (var material in materials)
            {
                var activeAssignment = material.MaterialAssignments?.FirstOrDefault(ma => ma.IsActive);
                if (activeAssignment != null)
                {
                    // Get latest condition
                    var latestCondition = activeAssignment.MaterialConditions?.FirstOrDefault();
                    if (latestCondition != null)
                    {
                        viewModel.MaterialConditions[material.Id] = latestCondition;
                    }

                    // Load station/department names
                    await LoadLocationData(viewModel, activeAssignment);

                    // Load employee info
                    if (activeAssignment.PayrollNo != null)
                    {
                        await LoadEmployeeInfo(viewModel, activeAssignment.PayrollNo);
                    }
                }

                // Load media
                await LoadMediaData(viewModel, material);
            }

            // Load vendors
            var vendors = await _vendorService.GetVendorsAsync();
            viewModel.VendorNames = vendors.ToDictionary(v => v.VendorID.ToString(), v => v.Name);

            return View(viewModel);
        }

        private async Task LoadLocationData(MaterialsIndexViewModel viewModel, MaterialAssignment assignment)
        {
            if (assignment.StationId.HasValue && !viewModel.StationNames.ContainsKey(assignment.StationId.Value))
            {
                viewModel.StationNames[assignment.StationId.Value] = assignment.StationId.Value == 0
                    ? "Head Quarters (HQ)"
                    : (await _locationService.GetStationByIdAsync(assignment.StationId.Value))?.StationName ?? "Unknown";
            }

            if (assignment.DepartmentId.HasValue && !viewModel.DepartmentNames.ContainsKey(assignment.DepartmentId.Value))
            {
                var department = await _locationService.GetDepartmentByIdAsync(assignment.DepartmentId.Value.ToString());
                viewModel.DepartmentNames[assignment.DepartmentId.Value] = department?.DepartmentName ?? "Unknown";
            }
        }

        private async Task LoadEmployeeInfo(MaterialsIndexViewModel viewModel, string payrollNo)
        {
            if (!viewModel.EmployeeInfo.ContainsKey(payrollNo))
            {
                try
                {
                    var employee = await _employeeService.GetEmployeeByPayrollAsync(payrollNo);
                    viewModel.EmployeeInfo[payrollNo] = employee != null
                        ? (employee.Fullname, employee.Designation)
                        : ("Not Assigned", "-");
                }
                catch
                {
                    viewModel.EmployeeInfo[payrollNo] = ("Unknown", "Unknown");
                }
            }
        }

        private async Task LoadMediaData(MaterialsIndexViewModel viewModel, Material material)
        {
            // Material image
            var materialMedia = await _mediaService.GetFirstMediaForModelAsync("Material", material.Id);
            if (materialMedia != null)
            {
                viewModel.MaterialImageUrls[material.Id] = $"/{materialMedia.FilePath}";
            }

            // Category image
            if (material.MaterialCategory != null)
            {
                var categoryMedia = await _mediaService.GetFirstMediaForModelAsync("MaterialCategory", material.MaterialCategoryId);
                if (categoryMedia != null)
                {
                    viewModel.CategoryImageUrls[material.Id] = $"/{categoryMedia.FilePath}";
                }
            }
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

            // Create the view model
            var viewModel = new MaterialDisplayViewModel
            {
                // Basic Material Information
                Id = material.Id,
                Code = material.Code ?? string.Empty,
                Name = material.Name ?? string.Empty,
                Description = material.Description ?? string.Empty,

                // Category Information
                MaterialCategoryId = material.MaterialCategoryId,
                MaterialCategoryName = material.MaterialCategory?.Name ?? "Unknown",
                MaterialSubcategoryId = material.MaterialSubcategoryId,
                MaterialSubcategoryName = material.MaterialSubcategory?.Name ?? "N/A",

                // Vendor/Supplier Information
                VendorId = material.VendorId ?? string.Empty,
                VendorName = material.VendorId != null && vendorNames.TryGetValue(material.VendorId, out string vendorName) ? vendorName : "None",

                // Status and Location
                Status = material.Status,

                // Purchase Information
                PurchaseDate = material.PurchaseDate,
                PurchasePrice = material.PurchasePrice,

                // Warranty Information
                WarrantyStartDate = material.WarrantyStartDate,
                WarrantyEndDate = material.WarrantyEndDate,
                WarrantyTerms = material.WarrantyTerms,

                // Lifecycle Management
                ExpectedLifespanMonths = material.ExpectedLifespanMonths,
                MaintenanceIntervalMonths = material.MaintenanceIntervalMonths,
                LastMaintenanceDate = material.LastMaintenanceDate,
                NextMaintenanceDate = material.NextMaintenanceDate,

                // Additional Metadata
                Manufacturer = material.Manufacturer,
                ModelNumber = material.ModelNumber,
                QRCode = material.QRCODE,
                AssetTag = material.AssetTag,
                Specifications = material.Specifications,

                // Timestamps
                CreatedAt = material.CreatedAt,
                UpdatedAt = material.UpdatedAt
            };

            // Get active assignment
            var activeAssignment = material.MaterialAssignments?.FirstOrDefault(ma => ma.IsActive);
            if (activeAssignment != null)
            {
                viewModel.CurrentAssignmentId = activeAssignment.Id;
                viewModel.AssignedToPayrollNo = activeAssignment.PayrollNo;
                viewModel.AssignmentDate = activeAssignment.AssignmentDate;
                viewModel.AssignmentType = activeAssignment.AssignmentType.ToString();
                viewModel.AssignedByPayrollNo = activeAssignment.AssignedByPayrollNo;
                viewModel.AssignmentNotes = activeAssignment.Notes;

                // Location Information
                viewModel.StationCategory = activeAssignment.StationCategory;
                viewModel.StationId = activeAssignment.StationId;
                viewModel.DepartmentId = activeAssignment.DepartmentId;
                viewModel.SpecificLocation = activeAssignment.SpecificLocation;

                // Get station and department names
                if (activeAssignment.StationId.HasValue)
                {
                    // Special case for HQ
                    if (activeAssignment.StationId.Value == 0)
                    {
                        viewModel.StationName = "Head Quarters (HQ)";
                    }
                    else
                    {
                        try
                        {
                            var station = await _locationService.GetStationByIdAsync(activeAssignment.StationId.Value);
                            if (station != null)
                            {
                                viewModel.StationName = station.StationName;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error loading station name: {ex.Message}");
                        }
                    }
                }

                if (activeAssignment.DepartmentId.HasValue)
                {
                    try
                    {
                        var departmentId = activeAssignment.DepartmentId.Value.ToString();
                        var department = await _locationService.GetDepartmentByIdAsync(departmentId);
                        if (department != null)
                        {
                            viewModel.DepartmentName = department.DepartmentName;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading department name: {ex.Message}");
                    }
                }
            }

            // Get latest condition
            var latestCondition = material.MaterialConditions?.OrderByDescending(mc => mc.InspectionDate).FirstOrDefault();
            if (latestCondition != null)
            {
                viewModel.CurrentConditionId = latestCondition.Id;
                viewModel.CurrentCondition = latestCondition.Condition;
                viewModel.FunctionalStatus = latestCondition.FunctionalStatus;
                viewModel.CosmeticStatus = latestCondition.CosmeticStatus;
                viewModel.InspectedBy = latestCondition.InspectedBy;
                viewModel.InspectionDate = latestCondition.InspectionDate;
                viewModel.ConditionNotes = latestCondition.Notes;
            }

            // Load media
            var mainImage = await _mediaService.GetFirstMediaForModelAsync("Material", material.Id);
            if (mainImage != null)
            {
                viewModel.MainImagePath = mainImage.FilePath;
            }
            else if (material.MaterialCategory != null)
            {
                // Use category image as fallback
                var categoryImage = await _mediaService.GetFirstMediaForModelAsync("MaterialCategory", material.MaterialCategoryId);
                if (categoryImage != null)
                {
                    viewModel.MainImagePath = categoryImage.FilePath;
                }
            }

            // Load gallery images
            var galleryImages = await _mediaService.GetMediaForModelAsync("Material", material.Id, "gallery");
            if (galleryImages != null)
            {
                viewModel.GalleryImagePaths = galleryImages.Select(img => img.FilePath).ToList();
            }

            // Convert assignment history
            if (material.MaterialAssignments != null)
            {
                viewModel.AssignmentHistory = material.MaterialAssignments
                    .OrderByDescending(ma => ma.AssignmentDate)
                    .Select(ma => new MaterialAssignmentViewModel
                    {
                        Id = ma.Id,
                        MaterialId = ma.MaterialId,
                        PayrollNo = ma.PayrollNo,
                        AssignmentDate = ma.AssignmentDate,
                        ReturnDate = ma.ReturnDate,
                        StationCategory = ma.StationCategory,
                        StationId = ma.StationId,
                        DepartmentId = ma.DepartmentId,
                        SpecificLocation = ma.SpecificLocation,
                        AssignmentType = ma.AssignmentType,
                        RequisitionId = ma.RequisitionId,
                        AssignedByPayrollNo = ma.AssignedByPayrollNo,
                        Notes = ma.Notes,
                        IsActive = ma.IsActive
                    }).ToList();
            }

            // Convert condition history
            if (material.MaterialConditions != null)
            {
                viewModel.ConditionHistory = material.MaterialConditions
                    .OrderByDescending(mc => mc.InspectionDate)
                    .Select(mc => new MaterialConditionViewModel
                    {
                        Id = mc.Id,
                        MaterialId = mc.MaterialId,
                        MaterialAssignmentId = mc.MaterialAssignmentId,
                        RequisitionId = mc.RequisitionId,
                        RequisitionItemId = mc.RequisitionItemId,
                        ConditionCheckType = mc.ConditionCheckType,
                        Stage = mc.Stage,
                        Condition = mc.Condition,
                        FunctionalStatus = mc.FunctionalStatus,
                        CosmeticStatus = mc.CosmeticStatus,
                        ComponentStatuses = mc.ComponentStatuses,
                        Notes = mc.Notes,
                        InspectedBy = mc.InspectedBy,
                        InspectionDate = mc.InspectionDate,
                        ActionRequired = mc.ActionRequired,
                        ActionDueDate = mc.ActionDueDate
                    }).ToList();
            }

            return View(viewModel);
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

                if (userStation != null)
                {
                    // Determine if user is at HQ/headoffice or factory/region
                    if (userStation.StationName?.ToLower() == "hq")
                    {
                        // User is at head office
                        viewModel.SelectedLocationCategory = "headoffice";
                        viewModel.Assignment.StationId = 0; // HQ has stationId 0
                    }
                    else if (userStation.StationName.Contains("region"))
                    {
                        viewModel.SelectedLocationCategory = "region";
                        viewModel.Assignment.StationId = userStation.StationId;
                    }
                    else
                    {
                        viewModel.SelectedLocationCategory = "factory";
                        viewModel.Assignment.StationId = userStation.StationId;
                    }

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
            viewModel.Material.Status = MaterialStatus.Available;

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
                            StationId = viewModel.Assignment?.StationId ?? 0,
                            DepartmentId = viewModel.Assignment?.DepartmentId ?? 0,
                            AssignmentType = viewModel.Assignment?.AssignmentType ?? RequisitionType.NewPurchase,
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
                            Condition = Condition.GoodCondition,
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
                // Use the StationCategoryService to get locations based on the category
                viewModel.Stations = await _stationCategoryService.GetLocationsForCategoryAsync(
                    viewModel.SelectedLocationCategory,
                    viewModel.StationId?.ToString());
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

            // First, get the material with basic information
            var material = await _context.Materials
                .Include(m => m.MaterialCategory)
                .Include(m => m.MaterialSubcategory)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (material == null)
            {
                return NotFound();
            }

            // Then, load active assignments separately
            var activeAssignments = await _context.MaterialAssignments
                .Where(ma => ma.MaterialId == id && ma.IsActive)
                .ToListAsync();

            // Load recent assignments separately
            var recentAssignments = await _context.MaterialAssignments
                .Where(ma => ma.MaterialId == id)
                .OrderByDescending(ma => ma.AssignmentDate)
                .Take(5)
                .ToListAsync();

            // Load recent conditions separately
            var recentConditions = await _context.MaterialConditions
                .Where(mc => mc.MaterialId == id)
                .OrderByDescending(mc => mc.InspectionDate)
                .Take(5)
                .ToListAsync();

            // Manually set the navigation properties
            material.MaterialAssignments = new List<MaterialAssignment>();
            material.MaterialAssignments = material.MaterialAssignments.Concat(activeAssignments).Concat(recentAssignments).Distinct().ToList();
            material.MaterialConditions = recentConditions;

            // Load media files for the material
            var mainImage = await _mediaService.GetFirstMediaForModelAsync("Material", material.Id);
            var galleryImages = await _mediaService.GetMediaForModelAsync("Material", material.Id, "gallery");

            var viewModel = new EditMaterialViewModel
            {
                Material = material,
                AssignmentHistory = recentAssignments.OrderByDescending(ma => ma.AssignmentDate),
                ConditionHistory = recentConditions.OrderByDescending(mc => mc.InspectionDate),
                ExistingMainImage = mainImage,
                ExistingGalleryImages = galleryImages.ToList()
            };

            // Get the active assignment
            var activeAssignment = material.MaterialAssignments?.FirstOrDefault(ma => ma.IsActive);
            if (activeAssignment != null)
            {
                viewModel.Assignment = activeAssignment;
                viewModel.SelectedLocationCategory = activeAssignment.StationCategory;
                viewModel.StationId = activeAssignment.StationId;

            }

            // Initialize a new condition record
            viewModel.Condition = new MaterialCondition
            {
                MaterialId = material.Id,
                MaterialAssignmentId = activeAssignment?.Id,
                ConditionCheckType = ConditionCheckType.Periodic,
                Stage = "Update",
                Condition = Condition.GoodCondition,
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
                // Use the StationCategoryService to get locations based on the category
                viewModel.Stations = await _stationCategoryService.GetLocationsForCategoryAsync(
                    viewModel.SelectedLocationCategory,
                    viewModel.StationId?.ToString());
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

            // Ensure required fields are set for MaterialCondition
            if (viewModel.Condition != null)
            {
                // Set required fields for MaterialCondition
                if (viewModel.Condition.ConditionCheckType == 0) // Default enum value
                {
                    viewModel.Condition.ConditionCheckType = ConditionCheckType.Periodic;
                    ModelState.Remove("Condition.ConditionCheckType");
                }

                if (string.IsNullOrEmpty(viewModel.Condition.Stage))
                {
                    viewModel.Condition.Stage = "Update";
                    ModelState.Remove("Condition.Stage");
                }
            }

            // Ensure AssignedByPayrollNo is set
            if (viewModel.Assignment != null && string.IsNullOrEmpty(viewModel.Assignment.AssignedByPayrollNo))
            {
                viewModel.Assignment.AssignedByPayrollNo = HttpContext.Session.GetString("EmployeePayrollNo") ?? "System";
                ModelState.Remove("Assignment.AssignedByPayrollNo");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingMaterial = await _context.Materials
                      .Include(m => m.MaterialAssignments.Where(ma => ma.IsActive))
                      .FirstOrDefaultAsync(m => m.Id == id);

                    var latestCondition = await _context.MaterialConditions
                        .Where(mc => mc.MaterialId == existingMaterial.Id)
                        .OrderByDescending(mc => mc.InspectionDate)
                        .FirstOrDefaultAsync();

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
                    existingMaterial.QRCODE = viewModel.Material.QRCODE;
                    existingMaterial.AssetTag = viewModel.Material.AssetTag;
                    existingMaterial.Specifications = viewModel.Material.Specifications;

                    // Update the material
                    _context.Update(existingMaterial);

                    // Check if location has changed
                    bool locationChanged = activeAssignment == null ||
                                          activeAssignment.StationCategory != viewModel.SelectedLocationCategory ||
                                          activeAssignment.StationId != viewModel.StationId;

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
                            StationId = viewModel.StationId,
                            DepartmentId = viewModel.Assignment.DepartmentId,
                            AssignmentType = viewModel.Assignment?.AssignmentType ?? RequisitionType.NewPurchase,
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
                            Condition = latestCondition.Condition,
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
                            Condition = latestCondition.Condition,
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
                // Use the StationCategoryService to get locations based on the category
                viewModel.Stations = await _stationCategoryService.GetLocationsForCategoryAsync(
                    viewModel.SelectedLocationCategory,
                    viewModel.StationId?.ToString());
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
