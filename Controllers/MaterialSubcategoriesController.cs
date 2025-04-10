using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MRIV.Attributes;
using MRIV.Models;

namespace MRIV.Controllers
{
    [CustomAuthorize]
    public class MaterialSubcategoriesController : Controller
    {
        private readonly RequisitionContext _context;

        public MaterialSubcategoriesController(RequisitionContext context)
        {
            _context = context;
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

        private bool MaterialSubcategoryExists(int id)
        {
            return _context.MaterialSubCategories.Any(e => e.Id == id);
        }
    }
}
