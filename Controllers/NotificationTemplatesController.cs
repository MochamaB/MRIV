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
    public class NotificationTemplatesController : Controller
    {
        private readonly RequisitionContext _context;

        public NotificationTemplatesController(RequisitionContext context)
        {
            _context = context;
        }

        // GET: NotificationTemplates
        public async Task<IActionResult> Index()
        {
            return View(await _context.NotificationTemplates.ToListAsync());
        }

        // GET: NotificationTemplates/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var notificationTemplate = await _context.NotificationTemplates
                .FirstOrDefaultAsync(m => m.Id == id);
            if (notificationTemplate == null)
            {
                return NotFound();
            }

            return View(notificationTemplate);
        }

        // GET: NotificationTemplates/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: NotificationTemplates/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,TitleTemplate,MessageTemplate,NotificationType")] NotificationTemplate notificationTemplate)
        {
            if (ModelState.IsValid)
            {
                _context.Add(notificationTemplate);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(notificationTemplate);
        }

        // GET: NotificationTemplates/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var notificationTemplate = await _context.NotificationTemplates.FindAsync(id);
            if (notificationTemplate == null)
            {
                return NotFound();
            }
            return View(notificationTemplate);
        }

        // POST: NotificationTemplates/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,TitleTemplate,MessageTemplate,NotificationType")] NotificationTemplate notificationTemplate)
        {
            if (id != notificationTemplate.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(notificationTemplate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NotificationTemplateExists(notificationTemplate.Id))
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
            return View(notificationTemplate);
        }

        // GET: NotificationTemplates/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var notificationTemplate = await _context.NotificationTemplates
                .FirstOrDefaultAsync(m => m.Id == id);
            if (notificationTemplate == null)
            {
                return NotFound();
            }

            return View(notificationTemplate);
        }

        // POST: NotificationTemplates/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var notificationTemplate = await _context.NotificationTemplates.FindAsync(id);
            if (notificationTemplate != null)
            {
                _context.NotificationTemplates.Remove(notificationTemplate);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool NotificationTemplateExists(int id)
        {
            return _context.NotificationTemplates.Any(e => e.Id == id);
        }
    }
}
