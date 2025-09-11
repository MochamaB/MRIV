using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MRIV.Attributes;
using MRIV.Models;
using MRIV.Services;
using MRIV.ViewModels;

namespace MRIV.Controllers
{
    [CustomAuthorize]
    public class MaterialSubcategoriesController : Controller
    {
        private readonly RequisitionContext _context;
        private readonly IMaterialImportService _importService;

        public MaterialSubcategoriesController(RequisitionContext context, IMaterialImportService importService)
        {
            _context = context;
            _importService = importService;
        }

        // GET: MaterialSubcategories
        public async Task<IActionResult> Index()
        {
            var requisitionContext = _context.MaterialSubCategories.Include(m => m.MaterialCategory);
            return View(await requisitionContext.ToListAsync());
        }

        // GET: MaterialSubcategories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var materialSubcategory = await _context.MaterialSubCategories
                .Include(m => m.MaterialCategory)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (materialSubcategory == null)
            {
                return NotFound();
            }

            return View(materialSubcategory);
        }

        // GET: MaterialSubcategories/Create
        public IActionResult Create()
        {
            ViewData["MaterialCategoryId"] = new SelectList(_context.MaterialCategories, "Id", "Name");
            return View();
        }

        // POST: MaterialSubcategories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,MaterialCategoryId,Name,Description")] MaterialSubcategory materialSubcategory)
        {
            if (ModelState.IsValid)
            {
                _context.Add(materialSubcategory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaterialCategoryId"] = new SelectList(_context.MaterialCategories, "Id", "Name", materialSubcategory.MaterialCategoryId);
            return View(materialSubcategory);
        }

        // GET: MaterialSubcategories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var materialSubcategory = await _context.MaterialSubCategories.FindAsync(id);
            if (materialSubcategory == null)
            {
                return NotFound();
            }
            ViewData["MaterialCategoryId"] = new SelectList(_context.MaterialCategories, "Id", "Name", materialSubcategory.MaterialCategoryId);
            return View(materialSubcategory);
        }

        // POST: MaterialSubcategories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,MaterialCategoryId,Name,Description")] MaterialSubcategory materialSubcategory)
        {
            if (id != materialSubcategory.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(materialSubcategory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MaterialSubcategoryExists(materialSubcategory.Id))
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
            ViewData["MaterialCategoryId"] = new SelectList(_context.MaterialCategories, "Id", "Name", materialSubcategory.MaterialCategoryId);
            return View(materialSubcategory);
        }

        // GET: MaterialSubcategories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var materialSubcategory = await _context.MaterialSubCategories
                .Include(m => m.MaterialCategory)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (materialSubcategory == null)
            {
                return NotFound();
            }

            return View(materialSubcategory);
        }

        // POST: MaterialSubcategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var materialSubcategory = await _context.MaterialSubCategories.FindAsync(id);
            if (materialSubcategory != null)
            {
                _context.MaterialSubCategories.Remove(materialSubcategory);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: MaterialSubcategories/Import
        public IActionResult Import()
        {
            return View();
        }

        // POST: MaterialSubcategories/Import
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Please select a file to import.");
                return View();
            }

            var result = await _importService.ImportSubcategoriesAsync(file);
            return View("ImportResults", result);
        }

        // GET: MaterialSubcategories/DownloadSample
        public async Task<IActionResult> DownloadSample()
        {
            // Get all categories for reference
            var categories = await _context.MaterialCategories.OrderBy(c => c.Name).ToListAsync();
            
            var csvContent = "Name,Description,CategoryName\n" +
                           "Laptops,Portable computers,Computers\n" +
                           "Desktops,Desktop computers,Computers\n" +
                           "Laser Printers,Printing devices,Printers\n" +
                           "Inkjet Printers,Printing devices,Printers\n" +
                           "Mouse,Input device,IT Equipment\n" +
                           "Paper,Printing and writing paper,Office Supplies\n\n" +
                           "# Available Categories (use exact names in CategoryName column):\n";

            foreach (var category in categories)
            {
                csvContent += $"# {category.Name}\n";
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
            return File(bytes, "text/csv", "MaterialSubcategories_Sample.csv");
        }

        private bool MaterialSubcategoryExists(int id)
        {
            return _context.MaterialSubCategories.Any(e => e.Id == id);
        }
    }
}
