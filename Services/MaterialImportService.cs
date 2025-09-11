using Microsoft.EntityFrameworkCore;
using MRIV.Models;
using MRIV.ViewModels;
using System.Text;

namespace MRIV.Services
{
    public interface IMaterialImportService
    {
        Task<MaterialImportViewModel> ImportCategoriesAsync(IFormFile file);
        Task<MaterialImportViewModel> ImportSubcategoriesAsync(IFormFile file);
    }

    public class MaterialImportService : IMaterialImportService
    {
        private readonly RequisitionContext _context;

        public MaterialImportService(RequisitionContext context)
        {
            _context = context;
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
    }
}
