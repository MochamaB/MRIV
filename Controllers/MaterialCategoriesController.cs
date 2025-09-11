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
using Microsoft.AspNetCore.Http;

namespace MRIV.Controllers
{
    [CustomAuthorize]
    public class MaterialCategoriesController : Controller
    {
        private readonly RequisitionContext _context;
        private readonly IMediaService _mediaService;
        private readonly IMaterialImportService _importService;

        public MaterialCategoriesController(RequisitionContext context, IMediaService mediaService, IMaterialImportService importService)
        {
            _context = context;
            _mediaService = mediaService;
            _importService = importService;
        }

        // GET: MaterialCategories
        public async Task<IActionResult> Index()
        {
            var categories = await _context.MaterialCategories.ToListAsync();
            
            // Load the first media file for each category
            foreach (var category in categories)
            {
                var media = await _mediaService.GetFirstMediaForModelAsync("MaterialCategory", category.Id);
                if (media != null)
                {
                    category.Media.Add(media);
                }
            }
            
            return View(categories);
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

            // Load media for this category
            var media = await _mediaService.GetMediaForModelAsync("MaterialCategory", materialCategory.Id);
            foreach (var item in media)
            {
                materialCategory.Media.Add(item);
            }

            return View(materialCategory);
        }

        // GET: MaterialCategories/Create
        public IActionResult Create()
        {
            var viewModel = new MaterialCategoryViewModel
            {
                Category = new MaterialCategory()
            };
            
            return View(viewModel);
        }

        // POST: MaterialCategories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MaterialCategoryViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                _context.Add(viewModel.Category);
                await _context.SaveChangesAsync();
                
                // Handle image upload if provided
                if (viewModel.ImageFile != null)
                {
                    await _mediaService.SaveMediaFileAsync(
                        viewModel.ImageFile,
                        "MaterialCategory",
                        viewModel.Category.Id,
                        "images");
                }
                
                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
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
            
            var viewModel = new MaterialCategoryViewModel
            {
                Category = materialCategory,
                ExistingImage = await _mediaService.GetFirstMediaForModelAsync("MaterialCategory", materialCategory.Id)
            };
            
            return View(viewModel);
        }

        // POST: MaterialCategories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MaterialCategoryViewModel viewModel)
        {
            if (id != viewModel.Category.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(viewModel.Category);
                    await _context.SaveChangesAsync();
                    
                    // Handle image upload if provided
                    if (viewModel.ImageFile != null)
                    {
                        // Delete existing image first
                        var existingMedia = await _mediaService.GetFirstMediaForModelAsync("MaterialCategory", viewModel.Category.Id);
                        if (existingMedia != null)
                        {
                            await _mediaService.DeleteMediaFileAsync(existingMedia.Id);
                        }
                        
                        // Save new image
                        await _mediaService.SaveMediaFileAsync(
                            viewModel.ImageFile,
                            "MaterialCategory",
                            viewModel.Category.Id,
                            "images");
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MaterialCategoryExists(viewModel.Category.Id))
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
            
            // If we got this far, something failed, redisplay form
            viewModel.ExistingImage = await _mediaService.GetFirstMediaForModelAsync("MaterialCategory", viewModel.Category.Id);
            return View(viewModel);
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

            // Load media for this category
            var media = await _mediaService.GetMediaForModelAsync("MaterialCategory", materialCategory.Id);
            foreach (var item in media)
            {
                materialCategory.Media.Add(item);
            }

            return View(materialCategory);
        }

        // POST: MaterialCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var materialCategory = await _context.MaterialCategories.FindAsync(id);
            if (materialCategory == null)
            {
                return NotFound();
            }
            
            // Delete associated media files
            var mediaFiles = await _mediaService.GetMediaForModelAsync("MaterialCategory", id);
            foreach (var mediaFile in mediaFiles)
            {
                await _mediaService.DeleteMediaFileAsync(mediaFile.Id);
            }
            
            _context.MaterialCategories.Remove(materialCategory);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: MaterialCategories/Import
        public IActionResult Import()
        {
            return View();
        }

        // POST: MaterialCategories/Import
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Please select a file to import.");
                return View();
            }

            var result = await _importService.ImportCategoriesAsync(file);
            return View("ImportResults", result);
        }

        // GET: MaterialCategories/DownloadSample
        public IActionResult DownloadSample()
        {
            var csvContent = "Name,Description,UnitOfMeasure\n" +
                           "IT Equipment,Computer hardware and accessories,Each\n" +
                           "Office Supplies,General office consumables,Pack\n" +
                           "Furniture,Office furniture and fixtures,Each\n" +
                           "Stationery,Writing and paper materials,Box\n" +
                           "Cleaning Supplies,Janitorial and maintenance items,Pack";

            var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
            return File(bytes, "text/csv", "MaterialCategories_Sample.csv");
        }

        private bool MaterialCategoryExists(int id)
        {
            return _context.MaterialCategories.Any(e => e.Id == id);
        }
    }
}
