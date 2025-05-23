using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MRIV.Data;
using MRIV.Models;
using MRIV.Services;
using MRIV.ViewModels;
using System.Text.Json;
using MRIV.Attributes;

namespace MRIV.Controllers
{
    [CustomAuthorize]
    public class WorkflowConfigsController : Controller
    {
        private readonly RequisitionContext _context;
        private readonly KtdaleaveContext _ktdaleaveContext;
        private readonly IStationCategoryService _stationCategoryService;

        public WorkflowConfigsController(RequisitionContext context,KtdaleaveContext ktdaleaveContext, IStationCategoryService stationCategoryService)
        {
            _context = context;
            _stationCategoryService = stationCategoryService;
            _ktdaleaveContext = ktdaleaveContext;
        }

        // GET: WorkflowConfigs
        public async Task<IActionResult> Index()
        {
            var workflows = await _context.Set<WorkflowConfig>()
                .Include(w => w.Steps)
                .ToListAsync();

            return View(workflows);
        }

        // GET: WorkflowConfigs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workflowConfig = await _context.Set<WorkflowConfig>()
                .Include(w => w.Steps.OrderBy(s => s.StepOrder))
                .FirstOrDefaultAsync(m => m.Id == id);

            if (workflowConfig == null)
            {
                return NotFound();
            }

            return View(workflowConfig);
        }

        // GET: WorkflowConfigs/Create
        public async Task<IActionResult> CreateAsync()
        {
            var viewModel = new WorkflowConfigViewModel
            {
                WorkflowConfig = new WorkflowConfig(),
                // Example steps for initial creation
                Steps = new List<WorkflowStepConfig>
                {
                    new WorkflowStepConfig { StepOrder = 1, StepName = "Supervisor Approval", ApproverRole = "supervisor" }
                },

                IssueStationCategories = await _stationCategoryService.GetStationCategoriesSelectListAsync("issue"),
                DeliveryStationCategories = await _stationCategoryService.GetStationCategoriesSelectListAsync("delivery"),
                ApproverRoles = GetApproverRolesSelectList()
            };

            return View(viewModel);
        }

        // POST: WorkflowConfigs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WorkflowConfigViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Check if a similar workflow already exists
                var existingWorkflow = await _context.Set<WorkflowConfig>()
                    .FirstOrDefaultAsync(w =>
                        w.IssueStationCategory == viewModel.WorkflowConfig.IssueStationCategory &&
                        w.DeliveryStationCategory == viewModel.WorkflowConfig.DeliveryStationCategory);

                if (existingWorkflow != null)
                {
                    ModelState.AddModelError("", "A workflow with these station categories already exists.");
                    viewModel.IssueStationCategories = await _stationCategoryService.GetStationCategoriesSelectListAsync("issue");
                    viewModel.DeliveryStationCategories = await _stationCategoryService.GetStationCategoriesSelectListAsync("delivery");
                    viewModel.ApproverRoles = GetApproverRolesSelectList();
                    return View(viewModel);
                }

                var workflowConfig = viewModel.WorkflowConfig;
                workflowConfig.Steps = viewModel.Steps;

                _context.Add(workflowConfig);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            viewModel.IssueStationCategories = await _stationCategoryService.GetStationCategoriesSelectListAsync("issue");
            viewModel.DeliveryStationCategories = await _stationCategoryService.GetStationCategoriesSelectListAsync("delivery");
            viewModel.ApproverRoles = GetApproverRolesSelectList();
            return View(viewModel);
        }

        // GET: WorkflowConfigs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workflowConfig = await _context.Set<WorkflowConfig>()
                .Include(w => w.Steps.OrderBy(s => s.StepOrder))
                .FirstOrDefaultAsync(m => m.Id == id);

            if (workflowConfig == null)
            {
                return NotFound();
            }

            var viewModel = new WorkflowConfigViewModel
            {
                WorkflowConfig = workflowConfig,
                Steps = workflowConfig.Steps.ToList(),
                IssueStationCategories = await _stationCategoryService.GetStationCategoriesSelectListAsync("issue"),
                DeliveryStationCategories = await _stationCategoryService.GetStationCategoriesSelectListAsync("delivery"),
                ApproverRoles = GetApproverRolesSelectList()
            };

            return View(viewModel);
        }

        // POST: WorkflowConfigs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, WorkflowConfigViewModel viewModel)
        {
            if (id != viewModel.WorkflowConfig.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var workflowConfig = await _context.Set<WorkflowConfig>()
                        .Include(w => w.Steps)
                        .FirstOrDefaultAsync(w => w.Id == id);

                    if (workflowConfig == null)
                    {
                        return NotFound();
                    }

                    // Update workflow properties
                    workflowConfig.IssueStationCategory = viewModel.WorkflowConfig.IssueStationCategory;
                    workflowConfig.DeliveryStationCategory = viewModel.WorkflowConfig.DeliveryStationCategory;

                    // Remove steps that aren't in the viewModel
                    var stepsToRemove = workflowConfig.Steps
                        .Where(s => !viewModel.Steps.Any(vs => vs.Id == s.Id))
                        .ToList();

                    foreach (var step in stepsToRemove)
                    {
                        workflowConfig.Steps.Remove(step);
                        _context.Remove(step);
                    }

                    // Update existing steps and add new ones
                    foreach (var viewModelStep in viewModel.Steps)
                    {
                        if (viewModelStep.Id > 0)
                        {
                            // Update existing step
                            var existingStep = workflowConfig.Steps.FirstOrDefault(s => s.Id == viewModelStep.Id);
                            if (existingStep != null)
                            {
                                existingStep.StepOrder = viewModelStep.StepOrder;
                                existingStep.StepName = viewModelStep.StepName;
                                existingStep.ApproverRole = viewModelStep.ApproverRole;

                                // Parameters are handled via AJAX - no changes needed here
                            }
                        }
                        else
                        {
                            // Add new step
                            var newStep = new WorkflowStepConfig
                            {
                                StepOrder = viewModelStep.StepOrder,
                                StepName = viewModelStep.StepName,
                                ApproverRole = viewModelStep.ApproverRole,
                                Conditions = new Dictionary<string, string>(),
                                RoleParameters = new Dictionary<string, string>()
                            };
                            workflowConfig.Steps.Add(newStep);
                        }
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WorkflowConfigExists(viewModel.WorkflowConfig.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            viewModel.IssueStationCategories = await _stationCategoryService.GetStationCategoriesSelectListAsync("issue");
            viewModel.DeliveryStationCategories = await _stationCategoryService.GetStationCategoriesSelectListAsync("delivery");
            viewModel.ApproverRoles = GetApproverRolesSelectList();
            return View(viewModel);
        }

        // GET: WorkflowConfigs/Delete/5
        // GET: WorkflowConfig/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workflowConfig = await _context.Set<WorkflowConfig>()
                .Include(w => w.Steps)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (workflowConfig == null)
            {
                return NotFound();
            }

            return View(workflowConfig);
        }


        // POST: WorkflowConfigs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var workflowConfig = await _context.Set<WorkflowConfig>()
                .Include(w => w.Steps)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (workflowConfig != null)
            {
                _context.RemoveRange(workflowConfig.Steps);
                _context.Remove(workflowConfig);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> AddStepAsync(WorkflowConfigViewModel viewModel)
        {
            var steps = viewModel.Steps ?? new List<WorkflowStepConfig>();

            // Find the next step order
            int nextOrder = 1;
            if (steps.Any())
            {
                nextOrder = steps.Max(s => s.StepOrder) + 1;
            }

            // Add a new step
            steps.Add(new WorkflowStepConfig
            {
                StepOrder = nextOrder,
                StepName = "New Step",
                ApproverRole = "supervisor",
                RoleParameters = new Dictionary<string, string>(),
                Conditions = new Dictionary<string, string>()
            });

            viewModel.Steps = steps;
            viewModel.IssueStationCategories = await _stationCategoryService.GetStationCategoriesSelectListAsync("issue");
            viewModel.DeliveryStationCategories = await _stationCategoryService.GetStationCategoriesSelectListAsync("delivery");
            viewModel.ApproverRoles = GetApproverRolesSelectList();

            return View(viewModel.WorkflowConfig.Id > 0 ? "Edit" : "Create", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CloneStepAsync(WorkflowConfigViewModel viewModel, int stepIndex)
        {
            var steps = viewModel.Steps ?? new List<WorkflowStepConfig>();

            if (stepIndex < 0 || stepIndex >= steps.Count)
            {
                return BadRequest("Invalid step index");
            }

            // Find the step to clone
            var stepToClone = steps[stepIndex];

            // Find the next step order
            int nextOrder = steps.Max(s => s.StepOrder) + 1;

            // Create a clone with a new ID
            var clonedStep = new WorkflowStepConfig
            {
                StepOrder = nextOrder,
                StepName = stepToClone.StepName + " (Clone)",
                ApproverRole = stepToClone.ApproverRole,
                RoleParameters = new Dictionary<string, string>(stepToClone.RoleParameters),
                Conditions = new Dictionary<string, string>(stepToClone.Conditions)
            };

            steps.Add(clonedStep);

            viewModel.Steps = steps;
            viewModel.IssueStationCategories = await _stationCategoryService.GetStationCategoriesSelectListAsync("issue");
            viewModel.DeliveryStationCategories = await _stationCategoryService.GetStationCategoriesSelectListAsync("delivery");
            viewModel.ApproverRoles = GetApproverRolesSelectList();

            return View(viewModel.WorkflowConfig.Id > 0 ? "Edit" : "Create", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveStepAsync(WorkflowConfigViewModel viewModel, int stepIndex)
        {
            var steps = viewModel.Steps ?? new List<WorkflowStepConfig>();

            if (stepIndex < 0 || stepIndex >= steps.Count)
            {
                return BadRequest("Invalid step index");
            }

            steps.RemoveAt(stepIndex);

            // Reorder remaining steps
            for (int i = 0; i < steps.Count; i++)
            {
                steps[i].StepOrder = i + 1;
            }

            viewModel.Steps = steps;
            viewModel.IssueStationCategories = await _stationCategoryService.GetStationCategoriesSelectListAsync("issue");
            viewModel.DeliveryStationCategories = await _stationCategoryService.GetStationCategoriesSelectListAsync("delivery");
            viewModel.ApproverRoles = GetApproverRolesSelectList();

            return View(viewModel.WorkflowConfig.Id > 0 ? "Edit" : "Create", viewModel);
        }

        // GET: WorkflowConfigs/GetStepParameters
        [HttpGet]
        public IActionResult GetStepParameters(int workflowId, int stepIndex)
        {
            var workflow = _context.WorkflowConfigs
                .Include(w => w.Steps)
                .FirstOrDefault(w => w.Id == workflowId);

            if (workflow == null || stepIndex < 0 || stepIndex >= workflow.Steps.Count)
            {
                return Json(new { success = false, message = "Invalid workflow or step index" });
            }

            var step = workflow.Steps.ElementAt(stepIndex);
            
            // Create a tuple with the step parameters for the partial view
            var roleParameters = new Tuple<string, int, Dictionary<string, string>>(
                "RoleParameters", 
                stepIndex, 
                step.RoleParameters ?? new Dictionary<string, string>()
            );
            
            var conditions = new Tuple<string, int, Dictionary<string, string>>(
                "Conditions", 
                stepIndex, 
                step.Conditions ?? new Dictionary<string, string>()
            );
            
            ViewBag.StepIndex = stepIndex;
            
            return PartialView("_StepParametersModal", new Tuple<Tuple<string, int, Dictionary<string, string>>, Tuple<string, int, Dictionary<string, string>>>(
                roleParameters, conditions
            ));
        }

        // GET: WorkflowConfigs/GetStepParametersJson
        // GET: WorkflowConfigs/GetStepParametersJson
        [HttpGet]
        [Route("WorkflowConfigs/Edit/{id}/GetStepParametersJson")]
        public IActionResult GetStepParametersJson(int workflowId, int stepIndex)
        {
            var workflow = _context.WorkflowConfigs
                .Include(w => w.Steps)
                .FirstOrDefault(w => w.Id == workflowId);

            if (workflow == null || stepIndex < 0 || stepIndex >= workflow.Steps.Count)
            {
                return Json(new { success = false, message = "Invalid workflow or step index" });
            }

            var step = workflow.Steps.ElementAt(stepIndex);

            // Return parameters as simple dictionaries to avoid circular references
            return Json(new
            {
                success = true,
                stepId = step.Id,
                roleParameters = step.RoleParameters ?? new Dictionary<string, string>(),
                conditions = step.Conditions ?? new Dictionary<string, string>()
            });
        }

        // POST: WorkflowConfigs/SaveStepParameters
        [HttpPost]
        public IActionResult SaveStepParameters([FromBody] SaveStepParametersRequest request)
        {
            try
            {
                var workflowId = request.WorkflowId;
                var stepIndex = request.StepIndex;
                var model = request.Model;

                var workflow = _context.WorkflowConfigs
                    .Include(w => w.Steps)
                    .FirstOrDefault(w => w.Id == workflowId);

                if (workflow == null || stepIndex < 0 || stepIndex >= workflow.Steps.Count)
                {
                    return Json(new { success = false, message = "Invalid workflow or step index" });
                }

                var step = workflow.Steps.ElementAt(stepIndex);
                
                // Initialize dictionaries if they're null
                step.RoleParameters ??= new Dictionary<string, string>();
                step.Conditions ??= new Dictionary<string, string>();
                
                // Clear existing dictionaries to prevent stale data
                step.RoleParameters.Clear();
                step.Conditions.Clear();
                
                // Add role parameters
                if (model.RoleParameters != null && model.RoleParameters.Keys != null)
                {
                    for (int i = 0; i < model.RoleParameters.Keys.Count; i++)
                    {
                        string key = model.RoleParameters.Keys[i];
                        if (!string.IsNullOrEmpty(key))
                        {
                            string value = i < model.RoleParameters.Values.Count ? model.RoleParameters.Values[i] : "";
                            step.RoleParameters[key] = value;
                        }
                    }
                }
                
                // Add conditions
                if (model.Conditions != null && model.Conditions.Keys != null)
                {
                    for (int i = 0; i < model.Conditions.Keys.Count; i++)
                    {
                        string key = model.Conditions.Keys[i];
                        if (!string.IsNullOrEmpty(key))
                        {
                            string value = i < model.Conditions.Values.Count ? model.Conditions.Values[i] : "";
                            step.Conditions[key] = value;
                        }
                    }
                }
                
                // Save changes
                _context.Update(workflow);
                _context.SaveChanges();
                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: WorkflowConfigs/SaveStepParametersJson
        [HttpPost]
        [Route("WorkflowConfigs/Edit/{id}/SaveStepParametersJson")]
        public IActionResult SaveStepParametersJson([FromBody] SaveStepParametersJsonRequest request)
        {
            try
            {
                var workflow = _context.WorkflowConfigs
                    .Include(w => w.Steps)
                    .FirstOrDefault(w => w.Id == request.WorkflowId);

                if (workflow == null)
                {
                    return Json(new { success = false, message = "Workflow not found" });
                }

                // Find the step by ID instead of index for more reliability
                var step = workflow.Steps.FirstOrDefault(s => s.Id == request.StepId);

                // If step ID is 0 (new step), try finding by index
                if (step == null && request.StepId == 0)
                {
                    if (request.StepIndex < 0 || request.StepIndex >= workflow.Steps.Count)
                    {
                        return Json(new { success = false, message = "Invalid step index" });
                    }

                    step = workflow.Steps.ElementAt(request.StepIndex);
                }

                if (step == null)
                {
                    return Json(new { success = false, message = "Step not found" });
                }

                // Update parameters
                step.RoleParameters = request.RoleParameters ?? new Dictionary<string, string>();
                step.Conditions = request.Conditions ?? new Dictionary<string, string>();

                _context.Update(workflow);
                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Parameters saved successfully",
                    stepId = step.Id,
                    roleParametersJson = JsonSerializer.Serialize(step.RoleParameters),
                    conditionsJson = JsonSerializer.Serialize(step.Conditions)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class SaveStepParametersRequest
        {
            public int WorkflowId { get; set; }
            public int StepIndex { get; set; }
            public StepParametersViewModel Model { get; set; }
        }

        public class SaveStepParametersJsonRequest
        {
            public int WorkflowId { get; set; }
            public int StepId { get; set; }
            public int StepIndex { get; set; }
            public Dictionary<string, string> RoleParameters { get; set; }
            public Dictionary<string, string> Conditions { get; set; }
        }

        private bool WorkflowConfigExists(int id)
        {
            return _context.Set<WorkflowConfig>().Any(e => e.Id == id);
        }



        private SelectList GetApproverRolesSelectList()
        {
            try
            {
                // Get base roles from database
                var dbRoles = _ktdaleaveContext.Roles
                    .OrderBy(r => r.RoleName)
                    .Select(r => r.RoleName)
                    .ToList();

                // Create a combined list with additional roles
                var allRoles = new List<string>(dbRoles);

                // Add special roles if they don't already exist
                if (!allRoles.Contains("dispatchAdmin"))
                    allRoles.Add("dispatchAdmin");

                if (!allRoles.Contains("vendor"))
                    allRoles.Add("vendor");

                // Optional: Order the combined list
                allRoles = allRoles.OrderBy(r => r).ToList();

                return new SelectList(allRoles);
            }
            catch (Exception ex)
            {
                // Handle exceptions (log, return empty list, etc.)
                // Consider logging the exception here
                return new SelectList(Enumerable.Empty<string>());
            }
        }

    }
}
