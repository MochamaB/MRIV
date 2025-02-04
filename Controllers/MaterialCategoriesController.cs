using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MRIV.Models;

namespace MRIV.Controllers
{
    public class MaterialCategoriesController : Controller
    {
        private readonly RequisitionContext _context;

        public MaterialCategoriesController(RequisitionContext context)
        {
            _context = context;
        }

        // GET: MaterialCategories
        public async Task<IActionResult> Index()
        {
            return View(await _context.MaterialCategories.ToListAsync());
        }

        // GET: MaterialCategories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var materialCategory = await _context.MaterialCategories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (materialCategory == null)
            {
                return NotFound();
            }

            return View(materialCategory);
        }

        // GET: MaterialCategories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: MaterialCategories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,UnitOfMeasure")] MaterialCategory materialCategory)
        {
            if (ModelState.IsValid)
            {
                _context.Add(materialCategory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(materialCategory);
        }

        // GET: MaterialCategories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var materialCategory = await _context.MaterialCategories.FindAsync(id);
            if (materialCategory == null)
            {
                return NotFound();
            }
            return View(materialCategory);
        }

        // POST: MaterialCategories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,UnitOfMeasure")] MaterialCategory materialCategory)
        {
            if (id != materialCategory.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(materialCategory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MaterialCategoryExists(materialCategory.Id))
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
            return View(materialCategory);
        }

        // GET: MaterialCategories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var materialCategory = await _context.MaterialCategories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (materialCategory == null)
            {
                return NotFound();
            }

            return View(materialCategory);
        }

        // POST: MaterialCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var materialCategory = await _context.MaterialCategories.FindAsync(id);
            if (materialCategory != null)
            {
                _context.MaterialCategories.Remove(materialCategory);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MaterialCategoryExists(int id)
        {
            return _context.MaterialCategories.Any(e => e.Id == id);
        }
    }
}
