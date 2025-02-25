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
    public class WorkflowStepConfigsController : Controller
    {
        private readonly RequisitionContext _context;

        public WorkflowStepConfigsController(RequisitionContext context)
        {
            _context = context;
        }

        // GET: WorkflowStepConfigs
        public async Task<IActionResult> Index()
        {
            var requisitionContext = _context.WorkflowStepConfigs.Include(w => w.WorkflowConfig);
            return View(await requisitionContext.ToListAsync());
        }

        // GET: WorkflowStepConfigs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workflowStepConfig = await _context.WorkflowStepConfigs
                .Include(w => w.WorkflowConfig)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (workflowStepConfig == null)
            {
                return NotFound();
            }

            return View(workflowStepConfig);
        }

        // GET: WorkflowStepConfigs/Create
        public IActionResult Create()
        {
            ViewData["WorkflowConfigId"] = new SelectList(_context.WorkflowConfigs, "Id", "DeliveryStationCategory");
            return View();
        }

        // POST: WorkflowStepConfigs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,WorkflowConfigId,StepOrder,StepName,ApproverRole,RoleParameters,Conditions")] WorkflowStepConfig workflowStepConfig)
        {
            if (ModelState.IsValid)
            {
                _context.Add(workflowStepConfig);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["WorkflowConfigId"] = new SelectList(_context.WorkflowConfigs, "Id", "DeliveryStationCategory", workflowStepConfig.WorkflowConfigId);
            return View(workflowStepConfig);
        }

        // GET: WorkflowStepConfigs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workflowStepConfig = await _context.WorkflowStepConfigs.FindAsync(id);
            if (workflowStepConfig == null)
            {
                return NotFound();
            }
            ViewData["WorkflowConfigId"] = new SelectList(_context.WorkflowConfigs, "Id", "DeliveryStationCategory", workflowStepConfig.WorkflowConfigId);
            return View(workflowStepConfig);
        }

        // POST: WorkflowStepConfigs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,WorkflowConfigId,StepOrder,StepName,ApproverRole,RoleParameters,Conditions")] WorkflowStepConfig workflowStepConfig)
        {
            if (id != workflowStepConfig.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(workflowStepConfig);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WorkflowStepConfigExists(workflowStepConfig.Id))
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
            ViewData["WorkflowConfigId"] = new SelectList(_context.WorkflowConfigs, "Id", "DeliveryStationCategory", workflowStepConfig.WorkflowConfigId);
            return View(workflowStepConfig);
        }

        // GET: WorkflowStepConfigs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workflowStepConfig = await _context.WorkflowStepConfigs
                .Include(w => w.WorkflowConfig)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (workflowStepConfig == null)
            {
                return NotFound();
            }

            return View(workflowStepConfig);
        }

        // POST: WorkflowStepConfigs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var workflowStepConfig = await _context.WorkflowStepConfigs.FindAsync(id);
            if (workflowStepConfig != null)
            {
                _context.WorkflowStepConfigs.Remove(workflowStepConfig);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool WorkflowStepConfigExists(int id)
        {
            return _context.WorkflowStepConfigs.Any(e => e.Id == id);
        }
    }
}
