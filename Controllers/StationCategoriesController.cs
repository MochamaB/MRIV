using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MRIV.Attributes;
using MRIV.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MRIV.Controllers
{
    [CustomAuthorize]
    public class StationCategoriesController : Controller
    {
        private readonly RequisitionContext _context;

        public StationCategoriesController(RequisitionContext context)
        {
            _context = context;
        }

        // GET: StationCategories
        public async Task<IActionResult> Index()
        {
            return View(await _context.StationCategories.ToListAsync());
        }

        // GET: StationCategories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stationCategory = await _context.StationCategories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (stationCategory == null)
            {
                return NotFound();
            }

            return View(stationCategory);
        }

        // GET: StationCategories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: StationCategories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Code,StationName,StationPoint,DataSource,FilterCriteria")] StationCategory stationCategory)
        {
            if (ModelState.IsValid)
            {
                _context.Add(stationCategory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(stationCategory);
        }

        // GET: StationCategories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stationCategory = await _context.StationCategories.FindAsync(id);
            if (stationCategory == null)
            {
                return NotFound();
            }
            return View(stationCategory);
        }

        // POST: StationCategories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Code,StationName,StationPoint,DataSource,FilterCriteria")] StationCategory stationCategory)
        {
            if (id != stationCategory.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(stationCategory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StationCategoryExists(stationCategory.Id))
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
            return View(stationCategory);
        }

        // GET: StationCategories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stationCategory = await _context.StationCategories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (stationCategory == null)
            {
                return NotFound();
            }

            return View(stationCategory);
        }

        // POST: StationCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var stationCategory = await _context.StationCategories.FindAsync(id);
            if (stationCategory != null)
            {
                _context.StationCategories.Remove(stationCategory);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StationCategoryExists(int id)
        {
            return _context.StationCategories.Any(e => e.Id == id);
        }
    }
}
