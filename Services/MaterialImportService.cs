using Microsoft.EntityFrameworkCore;
using MRIV.Models;
using MRIV.ViewModels;
using MRIV.Enums;
using System.Text;

namespace MRIV.Services
{
    public interface IMaterialImportService
    {
        Task<MaterialImportViewModel> ImportCategoriesAsync(IFormFile file);
        Task<MaterialImportViewModel> ImportSubcategoriesAsync(IFormFile file);
        Task<MaterialImportViewModel> ImportMaterialsAsync(IFormFile file, string currentUserPayrollNo);
    }

    public class MaterialImportService : IMaterialImportService
    {
        private readonly RequisitionContext _context;
        private readonly KtdaleaveContext _ktdaContext;

        public MaterialImportService(RequisitionContext context, KtdaleaveContext ktdaContext)
        {
            _context = context;
            _ktdaContext = ktdaContext;
        }

        public async Task<MaterialImportViewModel> ImportCategoriesAsync(IFormFile file)
        {
            var result = new MaterialImportViewModel
            {
                ImportType = "Category",
                Results = new List<MaterialImportResult>()
            };

            if (file == null || file.Length == 0)
            {
                result.Results.Add(new MaterialImportResult
                {
                    RowNumber = 0,
                    IsSuccess = false,
                    ErrorMessage = "No file uploaded or file is empty"
                });
                result.HasResults = true;
                return result;
            }

            try
            {
                var categories = await ParseCategoriesFromFileAsync(file);
                var rowNumber = 1; // Start from 1 (assuming header row)

                foreach (var categoryRow in categories)
                {
                    rowNumber++;
                    var importResult = new MaterialImportResult
                    {
                        RowNumber = rowNumber,
                        Name = categoryRow.Name,
                        Description = categoryRow.Description,
                        AdditionalInfo = categoryRow.UnitOfMeasure
                    };

                    try
                    {
                        // Validate required fields
                        if (string.IsNullOrWhiteSpace(categoryRow.Name))
                        {
                            importResult.IsSuccess = false;
                            importResult.ErrorMessage = "Category name is required";
                            result.Results.Add(importResult);
                            continue;
                        }

                        // Check if category already exists
                        var existingCategory = await _context.MaterialCategories
                            .FirstOrDefaultAsync(c => c.Name.ToLower() == categoryRow.Name.ToLower());

                        if (existingCategory != null)
                        {
                            importResult.IsSuccess = false;
                            importResult.ErrorMessage = "Category with this name already exists";
                            result.Results.Add(importResult);
                            continue;
                        }

                        // Create new category
                        var newCategory = new MaterialCategory
                        {
                            Name = categoryRow.Name.Trim(),
                            Description = categoryRow.Description?.Trim(),
                            UnitOfMeasure = categoryRow.UnitOfMeasure?.Trim()
                        };

                        _context.MaterialCategories.Add(newCategory);
                        await _context.SaveChangesAsync();

                        importResult.IsSuccess = true;
                        result.Results.Add(importResult);
                        result.SuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        importResult.IsSuccess = false;
                        importResult.ErrorMessage = $"Error saving category: {ex.Message}";
                        result.Results.Add(importResult);
                    }
                }

                result.HasResults = true;
                result.TotalRows = categories.Count;
            }
            catch (Exception ex)
            {
                result.Results.Add(new MaterialImportResult
                {
                    RowNumber = 0,
                    IsSuccess = false,
                    ErrorMessage = $"Error processing file: {ex.Message}"
                });
                result.HasResults = true;
            }

            return result;
        }

        private async Task<List<MaterialCategoryImportRow>> ParseCategoriesFromFileAsync(IFormFile file)
        {
            var categories = new List<MaterialCategoryImportRow>();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            if (file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                categories = ParseCsvCategories(stream);
            }
            else if (file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) || 
                     file.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
            {
                // For now, we'll focus on CSV. Excel parsing can be added later with EPPlus or similar
                throw new NotSupportedException("Excel files not yet supported. Please use CSV format.");
            }
            else
            {
                throw new NotSupportedException("Only CSV files are supported");
            }

            return categories;
        }

        private List<MaterialCategoryImportRow> ParseCsvCategories(Stream stream)
        {
            var categories = new List<MaterialCategoryImportRow>();

            using var reader = new StreamReader(stream, Encoding.UTF8);
            var isFirstLine = true;

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                // Skip comment lines that start with #
                if (line.TrimStart().StartsWith("#")) continue;

                // Skip header row
                if (isFirstLine)
                {
                    isFirstLine = false;
                    continue;
                }

                var values = ParseCsvLine(line);
                if (values.Length >= 1) // At minimum, we need a name
                {
                    var category = new MaterialCategoryImportRow
                    {
                        Name = values.Length > 0 ? values[0]?.Trim() : "",
                        Description = values.Length > 1 ? values[1]?.Trim() : "",
                        UnitOfMeasure = values.Length > 2 ? values[2]?.Trim() : ""
                    };

                    categories.Add(category);
                }
            }

            return categories;
        }

        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var inQuotes = false;
            var currentField = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            result.Add(currentField.ToString());
            return result.ToArray();
        }

        public async Task<MaterialImportViewModel> ImportSubcategoriesAsync(IFormFile file)
        {
            var result = new MaterialImportViewModel
            {
                ImportType = "Subcategory",
                Results = new List<MaterialImportResult>()
            };

            if (file == null || file.Length == 0)
            {
                result.Results.Add(new MaterialImportResult
                {
                    RowNumber = 0,
                    IsSuccess = false,
                    ErrorMessage = "No file uploaded or file is empty"
                });
                result.HasResults = true;
                return result;
            }

            try
            {
                var subcategories = await ParseSubcategoriesFromFileAsync(file);
                var rowNumber = 1; // Start from 1 (assuming header row)

                foreach (var subcategoryRow in subcategories)
                {
                    rowNumber++;
                    var importResult = new MaterialImportResult
                    {
                        RowNumber = rowNumber,
                        Name = subcategoryRow.Name,
                        Description = subcategoryRow.Description,
                        AdditionalInfo = subcategoryRow.CategoryName
                    };

                    try
                    {
                        // Validate required fields
                        if (string.IsNullOrWhiteSpace(subcategoryRow.Name))
                        {
                            importResult.IsSuccess = false;
                            importResult.ErrorMessage = "Subcategory name is required";
                            result.Results.Add(importResult);
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(subcategoryRow.CategoryName))
                        {
                            importResult.IsSuccess = false;
                            importResult.ErrorMessage = "Category name is required";
                            result.Results.Add(importResult);
                            continue;
                        }

                        // Check if parent category exists
                        var parentCategory = await _context.MaterialCategories
                            .FirstOrDefaultAsync(c => c.Name.ToLower() == subcategoryRow.CategoryName.ToLower());

                        if (parentCategory == null)
                        {
                            importResult.IsSuccess = false;
                            importResult.ErrorMessage = $"Parent category '{subcategoryRow.CategoryName}' not found";
                            result.Results.Add(importResult);
                            continue;
                        }

                        // Check if subcategory already exists
                        var existingSubcategory = await _context.MaterialSubCategories
                            .FirstOrDefaultAsync(s => s.Name.ToLower() == subcategoryRow.Name.ToLower() && 
                                                     s.MaterialCategoryId == parentCategory.Id);

                        if (existingSubcategory != null)
                        {
                            importResult.IsSuccess = false;
                            importResult.ErrorMessage = "Subcategory with this name already exists in this category";
                            result.Results.Add(importResult);
                            continue;
                        }

                        // Create new subcategory
                        var newSubcategory = new MaterialSubcategory
                        {
                            Name = subcategoryRow.Name.Trim(),
                            Description = subcategoryRow.Description?.Trim(),
                            MaterialCategoryId = parentCategory.Id
                        };

                        _context.MaterialSubCategories.Add(newSubcategory);
                        await _context.SaveChangesAsync();

                        importResult.IsSuccess = true;
                        result.Results.Add(importResult);
                        result.SuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        importResult.IsSuccess = false;
                        importResult.ErrorMessage = $"Error saving subcategory: {ex.Message}";
                        result.Results.Add(importResult);
                    }
                }

                result.HasResults = true;
                result.TotalRows = subcategories.Count;
            }
            catch (Exception ex)
            {
                result.Results.Add(new MaterialImportResult
                {
                    RowNumber = 0,
                    IsSuccess = false,
                    ErrorMessage = $"Error processing file: {ex.Message}"
                });
                result.HasResults = true;
            }

            return result;
        }

        private async Task<List<MaterialSubcategoryImportRow>> ParseSubcategoriesFromFileAsync(IFormFile file)
        {
            var subcategories = new List<MaterialSubcategoryImportRow>();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            if (file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                subcategories = ParseCsvSubcategories(stream);
            }
            else if (file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) || 
                     file.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException("Excel files not yet supported. Please use CSV format.");
            }
            else
            {
                throw new NotSupportedException("Only CSV files are supported");
            }

            return subcategories;
        }

        private List<MaterialSubcategoryImportRow> ParseCsvSubcategories(Stream stream)
        {
            var subcategories = new List<MaterialSubcategoryImportRow>();

            using var reader = new StreamReader(stream, Encoding.UTF8);
            var isFirstLine = true;

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                // Skip comment lines that start with #
                if (line.TrimStart().StartsWith("#")) continue;

                // Skip header row
                if (isFirstLine)
                {
                    isFirstLine = false;
                    continue;
                }

                var values = ParseCsvLine(line);
                if (values.Length >= 1) // At minimum, we need a name
                {
                    var subcategory = new MaterialSubcategoryImportRow
                    {
                        Name = values.Length > 0 ? values[0]?.Trim() : "",
                        Description = values.Length > 1 ? values[1]?.Trim() : "",
                        CategoryName = values.Length > 2 ? values[2]?.Trim() : ""
                    };

                    subcategories.Add(subcategory);
                }
            }

            return subcategories;
        }

        public async Task<MaterialImportViewModel> ImportMaterialsAsync(IFormFile file, string currentUserPayrollNo)
        {
            var result = new MaterialImportViewModel
            {
                ImportType = "Material",
                Results = new List<MaterialImportResult>()
            };

            if (file == null || file.Length == 0)
            {
                result.Results.Add(new MaterialImportResult
                {
                    RowNumber = 0,
                    IsSuccess = false,
                    ErrorMessage = "No file uploaded or file is empty"
                });
                result.HasResults = true;
                return result;
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var materials = await ParseMaterialsFromFileAsync(file);
                    
                    // Load reference data for validation
                    var categories = await LoadCategoriesAsync();
                    var subcategories = await LoadSubcategoriesAsync();
                    var departments = await LoadDepartmentsAsync();
                    var employees = await LoadEmployeesAsync();
                    var stationCategories = await LoadStationCategoriesAsync();
                    
                    var rowNumber = 1; // Start from 1 (assuming header row)

                    foreach (var materialRow in materials)
                    {
                        rowNumber++;
                        try
                        {
                            // Validate required fields
                            if (string.IsNullOrWhiteSpace(materialRow.Name))
                            {
                                result.Results.Add(new MaterialImportResult
                                {
                                    RowNumber = rowNumber,
                                    IsSuccess = false,
                                ErrorMessage = "Material name is required"
                            });
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(materialRow.CategoryName))
                        {
                            result.Results.Add(new MaterialImportResult
                            {
                                RowNumber = rowNumber,
                                IsSuccess = false,
                                ErrorMessage = "Category name is required"
                            });
                            continue;
                        }

                        // Step 1: Create Material
                        var material = await CreateMaterialAsync(materialRow, categories, subcategories, currentUserPayrollNo);
                        if (material == null)
                        {
                            result.Results.Add(new MaterialImportResult
                            {
                                RowNumber = rowNumber,
                                IsSuccess = false,
                                ErrorMessage = "Failed to create material - invalid category or subcategory"
                            });
                            continue;
                        }

                        // Step 2: Create MaterialAssignment
                        var assignment = await CreateMaterialAssignmentAsync(material.Id, materialRow, departments, employees, stationCategories, currentUserPayrollNo);
                        
                        // Step 3: Create MaterialCondition
                        var condition = await CreateMaterialConditionAsync(material.Id, assignment.Id, materialRow, currentUserPayrollNo);

                        result.Results.Add(new MaterialImportResult
                        {
                            RowNumber = rowNumber,
                            IsSuccess = true,
                            ItemName = materialRow.Name,
                            Message = $"Material '{material.Name}' created successfully with code '{material.Code}'"
                        });
                    }
                    catch (Exception ex)
                    {
                        result.Results.Add(new MaterialImportResult
                        {
                            RowNumber = rowNumber,
                            IsSuccess = false,
                            ErrorMessage = $"Error processing row: {ex.Message}"
                        });
                    }
                }

                    await transaction.CommitAsync();
                    
                    result.HasResults = true;
                    result.TotalProcessed = result.Results.Count;
                    result.SuccessCount = result.Results.Count(r => r.IsSuccess);
                    result.FailureCount = result.Results.Count(r => !r.IsSuccess);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    result.Results.Add(new MaterialImportResult
                    {
                        RowNumber = 0,
                        IsSuccess = false,
                        ErrorMessage = $"File processing error: {ex.Message}"
                    });
                    result.HasResults = true;
                }
            });

            return result;
        }

        // Reference data loading methods
        private async Task<Dictionary<string, MaterialCategory>> LoadCategoriesAsync()
        {
            var categories = await _context.MaterialCategories.ToListAsync();
            return categories.ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);
        }

        private async Task<Dictionary<string, MaterialSubcategory>> LoadSubcategoriesAsync()
        {
            var subcategories = await _context.MaterialSubCategories
                .Include(s => s.MaterialCategory)
                .ToListAsync();
            return subcategories.ToDictionary(s => s.Name, s => s, StringComparer.OrdinalIgnoreCase);
        }

        private async Task<Dictionary<string, Department>> LoadDepartmentsAsync()
        {
            var departments = await _ktdaContext.Departments.ToListAsync();
            var dictionary = new Dictionary<string, Department>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var dept in departments)
            {
                // Add by exact name
                dictionary[dept.DepartmentName] = dept;
                
                // Add by ID as string for ID-based lookup
                dictionary[dept.DepartmentId] = dept;
                
                // Add common aliases/variations
                var aliases = GetDepartmentAliases(dept.DepartmentName);
                foreach (var alias in aliases)
                {
                    if (!dictionary.ContainsKey(alias))
                        dictionary[alias] = dept;
                }
            }
            
            return dictionary;
        }

        private List<string> GetDepartmentAliases(string departmentName)
        {
            var aliases = new List<string>();
            
            // Common IT department variations
            if (departmentName.Contains("INFORMATION") && departmentName.Contains("TECH"))
            {
                aliases.AddRange(new[] { "IT Department", "IT", "Information Technology", "ICT", "IT Dept" });
            }
            
            // Add more department aliases as needed
            // You can expand this based on your department names
            
            return aliases;
        }

        private async Task<Dictionary<string, EmployeeBkp>> LoadEmployeesAsync()
        {
            var employees = await _ktdaContext.EmployeeBkps.ToListAsync();
            return employees.ToDictionary(e => e.PayrollNo, e => e, StringComparer.OrdinalIgnoreCase);
        }

        private async Task<Dictionary<string, StationCategory>> LoadStationCategoriesAsync()
        {
            var stationCategories = await _context.StationCategories.ToListAsync();
            return stationCategories.ToDictionary(s => s.Code, s => s, StringComparer.OrdinalIgnoreCase);
        }

        // Material creation method
        private async Task<Material> CreateMaterialAsync(MaterialImportRow row, 
            Dictionary<string, MaterialCategory> categories,
            Dictionary<string, MaterialSubcategory> subcategories,
            string currentUserPayrollNo)
        {
            // Validate category
            if (!categories.TryGetValue(row.CategoryName, out var category))
            {
                return null;
            }

            // Validate subcategory if provided
            MaterialSubcategory subcategory = null;
            if (!string.IsNullOrWhiteSpace(row.SubcategoryName))
            {
                if (!subcategories.TryGetValue(row.SubcategoryName, out subcategory) || 
                    subcategory.MaterialCategoryId != category.Id)
                {
                    return null;
                }
            }

            // Generate code if not provided
            var code = string.IsNullOrWhiteSpace(row.Code) 
                ? await GenerateMaterialCodeAsync(category.Id) 
                : row.Code;

            // Check for duplicate code
            if (await _context.Materials.AnyAsync(m => m.Code == code))
            {
                throw new InvalidOperationException($"Material code '{code}' already exists");
            }

            // Parse dates
            DateTime? purchaseDate = ParseDate(row.PurchaseDate);
            DateTime? warrantyStartDate = ParseDate(row.WarrantyStartDate);
            DateTime? warrantyEndDate = ParseDate(row.WarrantyEndDate);

            // Parse decimal values
            decimal? purchasePrice = ParseDecimal(row.PurchasePrice);
            int? expectedLifespanMonths = ParseInt(row.ExpectedLifespanMonths);
            int? maintenanceIntervalMonths = ParseInt(row.MaintenanceIntervalMonths);

            // Parse status
            if (!Enum.TryParse<MaterialStatus>(row.Status, true, out var status))
            {
                status = MaterialStatus.Available;
            }

            var material = new Material
            {
                Name = row.Name,
                Code = code,
                Description = row.Description,
                MaterialCategoryId = category.Id,
                MaterialSubcategoryId = subcategory?.Id,
                Status = status,
                Manufacturer = row.Manufacturer,
                ModelNumber = row.ModelNumber,
                Specifications = row.Specifications,
                AssetTag = row.AssetTag,
                QRCODE = row.QRCODE,
                VendorId = row.VendorId,
                PurchaseDate = purchaseDate,
                PurchasePrice = purchasePrice,
                WarrantyStartDate = warrantyStartDate,
                WarrantyEndDate = warrantyEndDate,
                WarrantyTerms = row.WarrantyTerms,
                ExpectedLifespanMonths = expectedLifespanMonths,
                MaintenanceIntervalMonths = maintenanceIntervalMonths,
                CreatedAt = DateTime.UtcNow,
               
            };

            _context.Materials.Add(material);
            await _context.SaveChangesAsync();
            
            return material;
        }

        // MaterialAssignment creation method
        private async Task<MaterialAssignment> CreateMaterialAssignmentAsync(int materialId, 
            MaterialImportRow row,
            Dictionary<string, Department> departments,
            Dictionary<string, EmployeeBkp> employees,
            Dictionary<string, StationCategory> stationCategories,
            string currentUserPayrollNo)
        {
            // Validate employee if provided
            var payrollNo = string.IsNullOrWhiteSpace(row.AssignedToPayrollNo) || row.AssignedToPayrollNo == "NotAssigned" 
                ? "NotAssigned" 
                : row.AssignedToPayrollNo;

            if (payrollNo != "NotAssigned" && !employees.ContainsKey(payrollNo))
            {
                throw new InvalidOperationException($"Employee with payroll number '{payrollNo}' not found");
            }

            // Validate department if provided
            int? departmentId = null;

            if (!string.IsNullOrWhiteSpace(row.DepartmentName))
            {
                if (departments.TryGetValue(row.DepartmentName, out var department))
                {
                    if (int.TryParse(department.DepartmentId, out var parsedId))
                    {
                        departmentId = parsedId;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Department '{row.DepartmentName}' has an invalid ID '{department.DepartmentId}'"
                        );
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Department '{row.DepartmentName}' not found");
                }
            }


            // Parse assignment type
            if (!Enum.TryParse<RequisitionType>(row.AssignmentType, true, out var assignmentType))
            {
                assignmentType = RequisitionType.NewPurchase;
            }

            var assignment = new MaterialAssignment
            {
                MaterialId = materialId,
                PayrollNo = payrollNo,
                AssignmentDate = DateTime.UtcNow,
                StationCategory = row.StationCategory ?? "headoffice",
                StationId = 0, // Default for now
                DepartmentId = departmentId ?? 0,
                AssignmentType = assignmentType,
                AssignedByPayrollNo = currentUserPayrollNo,
                IsActive = true,
                Notes = row.AssignmentNotes ?? "Imported via CSV"
            };

            _context.MaterialAssignments.Add(assignment);
            await _context.SaveChangesAsync();
            
            return assignment;
        }

        // MaterialCondition creation method
        private async Task<MaterialCondition> CreateMaterialConditionAsync(int materialId, 
            int assignmentId, 
            MaterialImportRow row, 
            string currentUserPayrollNo)
        {
            // Parse condition check type
            if (!Enum.TryParse<ConditionCheckType>(row.ConditionCheckType, true, out var checkType))
            {
                checkType = ConditionCheckType.Initial;
            }

            // Parse condition status
            if (!Enum.TryParse<Condition>(row.ConditionStatus, true, out var conditionStatus))
            {
                conditionStatus = Condition.GoodCondition;
            }

            // Parse dates
            DateTime inspectionDate = ParseDate(row.InspectionDate) ?? DateTime.UtcNow;

            var condition = new MaterialCondition
            {
                MaterialId = materialId,
                MaterialAssignmentId = assignmentId,
                ConditionCheckType = checkType,
                Stage = row.Stage ?? "Import",
                Condition = conditionStatus,
                FunctionalStatus = FunctionalStatus.FullyFunctional,
                CosmeticStatus = CosmeticStatus.Excellent,
                InspectedBy = currentUserPayrollNo,
                InspectionDate = inspectionDate,
                Notes = row.ConditionNotes ?? "Initial condition recorded during import"
            };

            _context.MaterialConditions.Add(condition);
            await _context.SaveChangesAsync();
            
            return condition;
        }

        // Code generation method
        private async Task<string> GenerateMaterialCodeAsync(int categoryId)
        {
            var lastMaterial = await _context.Materials
                .Where(m => m.MaterialCategoryId == categoryId)
                .OrderByDescending(m => m.Id)
                .FirstOrDefaultAsync();

            var nextId = (lastMaterial?.Id ?? 0) + 1;
            return $"MAT-{nextId:D3}-{categoryId:D3}";
        }

        // Helper parsing methods
        private DateTime? ParseDate(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                return null;
            
            return DateTime.TryParse(dateString, out var date) ? date : null;
        }

        private decimal? ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            
            return decimal.TryParse(value, out var result) ? result : null;
        }

        private int? ParseInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            
            return int.TryParse(value, out var result) ? result : null;
        }

        private async Task<List<MaterialImportRow>> ParseMaterialsFromFileAsync(IFormFile file)
        {
            var materials = new List<MaterialImportRow>();

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                var headerLine = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    throw new InvalidOperationException("CSV file appears to be empty or invalid");
                }

                var headers = ParseCsvLine(headerLine);

                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    // Skip comment lines that start with #
                    if (line.TrimStart().StartsWith("#")) continue;

                    var values = ParseCsvLine(line);
                    
                    // Skip rows that are effectively empty (all values are empty or whitespace)
                    if (values.All(v => string.IsNullOrWhiteSpace(v))) continue;
                    
                    var material = new MaterialImportRow();

                    for (int i = 0; i < headers.Length && i < values.Length; i++)
                    {
                        var header = headers[i];
                        var value = values[i];

                        switch (header.ToLower())
                        {
                            case "name":
                                material.Name = value;
                                break;
                            case "categoryname":
                                material.CategoryName = value;
                                break;
                            case "subcategoryname":
                                material.SubcategoryName = value;
                                break;
                            case "code":
                                material.Code = value;
                                break;
                            case "description":
                                material.Description = value;
                                break;
                            case "status":
                                material.Status = value;
                                break;
                            case "manufacturer":
                                material.Manufacturer = value;
                                break;
                            case "modelnumber":
                                material.ModelNumber = value;
                                break;
                            case "specifications":
                                material.Specifications = value;
                                break;
                            case "assettag":
                                material.AssetTag = value;
                                break;
                            case "qrcode":
                                material.QRCODE = value;
                                break;
                            case "vendorid":
                                material.VendorId = value;
                                break;
                            case "purchasedate":
                                material.PurchaseDate = value;
                                break;
                            case "purchaseprice":
                                material.PurchasePrice = value;
                                break;
                            case "warrantystardate":
                                material.WarrantyStartDate = value;
                                break;
                            case "warrantyenddate":
                                material.WarrantyEndDate = value;
                                break;
                            case "warrantyterms":
                                material.WarrantyTerms = value;
                                break;
                            case "expectedlifespanmonths":
                                material.ExpectedLifespanMonths = value;
                                break;
                            case "maintenanceintervalmonths":
                                material.MaintenanceIntervalMonths = value;
                                break;
                            case "assignedtopayrollno":
                                material.AssignedToPayrollNo = value;
                                break;
                            case "stationcategory":
                                material.StationCategory = value;
                                break;
                            case "stationname":
                                material.StationName = value;
                                break;
                            case "departmentname":
                                material.DepartmentName = value;
                                break;
                            case "specificlocation":
                                material.SpecificLocation = value;
                                break;
                            case "assignmenttype":
                                material.AssignmentType = value;
                                break;
                            case "assignmentnotes":
                                material.AssignmentNotes = value;
                                break;
                            case "conditionstatus":
                                material.ConditionStatus = value;
                                break;
                            case "conditionnotes":
                                material.ConditionNotes = value;
                                break;
                            case "inspectiondate":
                                material.InspectionDate = value;
                                break;
                            case "conditionchecktype":
                                material.ConditionCheckType = value;
                                break;
                            case "stage":
                                material.Stage = value;
                                break;
                        }
                    }

                    materials.Add(material);
                }
            }

            return materials;
        }
    }
}
