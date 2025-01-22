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
    public class TbrequisitionsController : Controller
    {
        private readonly RequisitionContext _context;

        public TbrequisitionsController(RequisitionContext context)
        {
            _context = context;
        }

        // GET: Requisitions
        public async Task<IActionResult> Index(int page = 1, int pageSize = 15)
        {
            var totalRecords = await _context.Tbrequisitions.CountAsync();
            var requisitions = await _context.Tbrequisitions
                .OrderByDescending(r => r.Id) // Adjust based on the column for ordering
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Pass data to the view, including pagination metadata
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            return View(requisitions);
        }

        // GET: Requisitions/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tbrequisition = await _context.Tbrequisitions
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tbrequisition == null)
            {
                return NotFound();
            }

            return View(tbrequisition);
        }

        // GET: Requisitions/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Requisitions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Addedby,IssuedByApproval,ReqOfficerApproval,ReceivingOfficerApproval,Fwdapprove,ReqOfficer,RecOfficer,Code1,Item1,Issued1,Issuepoint,Usepoint,Issuedate,Sysdate,ReqUsername,External,SupComments,RecComments,Externalmail,Remarks1,RejectedSup,RejectedRec,Applicant,Recdate,SupDate,Admin,AdminOfficer,FwdDate,Adminapprove,AdminDate,AdminapproveDate,Receivedby,Collectorid,Collectorname")] Tbrequisition tbrequisition)
        {
            if (ModelState.IsValid)
            {
                _context.Add(tbrequisition);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tbrequisition);
        }

        // GET: Requisitions/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tbrequisition = await _context.Tbrequisitions.FindAsync(id);
            if (tbrequisition == null)
            {
                return NotFound();
            }
            return View(tbrequisition);
        }

        // POST: Requisitions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("Id,Addedby,IssuedByApproval,ReqOfficerApproval,ReceivingOfficerApproval,Fwdapprove,ReqOfficer,RecOfficer,Code1,Item1,Issued1,Issuepoint,Usepoint,Issuedate,Sysdate,ReqUsername,External,SupComments,RecComments,Externalmail,Remarks1,RejectedSup,RejectedRec,Applicant,Recdate,SupDate,Admin,AdminOfficer,FwdDate,Adminapprove,AdminDate,AdminapproveDate,Receivedby,Collectorid,Collectorname")] Tbrequisition tbrequisition)
        {
            if (id != tbrequisition.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tbrequisition);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TbrequisitionExists(tbrequisition.Id))
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
            return View(tbrequisition);
        }

        // GET: Requisitions/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tbrequisition = await _context.Tbrequisitions
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tbrequisition == null)
            {
                return NotFound();
            }

            return View(tbrequisition);
        }

        // POST: Requisitions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var tbrequisition = await _context.Tbrequisitions.FindAsync(id);
            if (tbrequisition != null)
            {
                _context.Tbrequisitions.Remove(tbrequisition);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TbrequisitionExists(long id)
        {
            return _context.Tbrequisitions.Any(e => e.Id == id);
        }
    }
}
