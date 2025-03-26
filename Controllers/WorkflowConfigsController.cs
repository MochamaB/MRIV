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

namespace MRIV.Controllers
{
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
                        // Process the dynamic parameters from form data
                        ProcessDynamicParameters(viewModelStep, Request.Form);
                        
                        if (viewModelStep.Id > 0)
                        {
                            // Update existing step
                            var existingStep = workflowConfig.Steps.FirstOrDefault(s => s.Id == viewModelStep.Id);
                            if (existingStep != null)
                            {
                                existingStep.StepOrder = viewModelStep.StepOrder;
                                existingStep.StepName = viewModelStep.StepName;
                                existingStep.ApproverRole = viewModelStep.ApproverRole;
                                existingStep.Conditions = viewModelStep.Conditions;
                                existingStep.RoleParameters = viewModelStep.RoleParameters;
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
                                Conditions = viewModelStep.Conditions,
                                RoleParameters = viewModelStep.RoleParameters
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
        [HttpGet]
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
            
            // Return just the parameters as simple dictionaries to avoid circular references
            return Json(new { 
                success = true, 
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
        public IActionResult SaveStepParametersJson([FromBody] SaveStepParametersJsonRequest request)
        {
            try
            {
                var workflowId = request.WorkflowId;
                var stepIndex = request.StepIndex;
                var roleParameters = request.RoleParameters;
                var conditions = request.Conditions;

                var workflow = _context.WorkflowConfigs
                    .Include(w => w.Steps)
                    .FirstOrDefault(w => w.Id == workflowId);

                if (workflow == null || stepIndex < 0 || stepIndex >= workflow.Steps.Count)
                {
                    return Json(new { success = false, message = "Invalid workflow or step index" });
                }

                var step = workflow.Steps.ElementAt(stepIndex);
                
                // Update role parameters
                step.RoleParameters = roleParameters ?? new Dictionary<string, string>();
                
                // Update conditions
                step.Conditions = conditions ?? new Dictionary<string, string>();
                
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

        public class SaveStepParametersRequest
        {
            public int WorkflowId { get; set; }
            public int StepIndex { get; set; }
            public StepParametersViewModel Model { get; set; }
        }

        public class SaveStepParametersJsonRequest
        {
            public int WorkflowId { get; set; }
            public int StepIndex { get; set; }
            public Dictionary<string, string> RoleParameters { get; set; }
            public Dictionary<string, string> Conditions { get; set; }
        }

        private bool WorkflowConfigExists(int id)
        {
            return _context.Set<WorkflowConfig>().Any(e => e.Id == id);
        }

        private void ProcessDynamicParameters(WorkflowStepConfig step, IFormCollection form)
        {
            // Initialize dictionaries if they're null
            step.RoleParameters ??= new Dictionary<string, string>();
            step.Conditions ??= new Dictionary<string, string>();
            
            // Clear existing dictionaries to prevent stale data
            step.RoleParameters.Clear();
            step.Conditions.Clear();
            
            // Get the step index (0-based in the form)
            int stepIndex = step.StepOrder - 1;
            
            // Process RoleParameters from the dynamic editor
            var roleParamKeys = form.Keys.Where(k => k.StartsWith($"Steps[{stepIndex}].RoleParameters_Keys")).ToList();
            var roleParamValues = form.Keys.Where(k => k.StartsWith($"Steps[{stepIndex}].RoleParameters_Values")).ToList();
            
            for (int i = 0; i < roleParamKeys.Count; i++)
            {
                string key = form[roleParamKeys[i]].ToString();
                if (!string.IsNullOrEmpty(key))
                {
                    string value = i < roleParamValues.Count ? form[roleParamValues[i]].ToString() : "";
                    step.RoleParameters[key] = value;
                }
            }
            
            // Process Conditions from the dynamic editor
            var conditionKeys = form.Keys.Where(k => k.StartsWith($"Steps[{stepIndex}].Conditions_Keys")).ToList();
            var conditionValues = form.Keys.Where(k => k.StartsWith($"Steps[{stepIndex}].Conditions_Values")).ToList();
            
            for (int i = 0; i < conditionKeys.Count; i++)
            {
                string key = form[conditionKeys[i]].ToString();
                if (!string.IsNullOrEmpty(key))
                {
                    string value = i < conditionValues.Count ? form[conditionValues[i]].ToString() : "";
                    step.Conditions[key] = value;
                }
            }
        }

        private SelectList GetApproverRolesSelectList()
        {
            try
            {
                // Assuming you have a Roles DbSet in your context
                var roles = _ktdaleaveContext.Roles
                    .OrderBy(r => r.RoleName)  // Optional: Order by role name
                    .Select(r => r.RoleName)   // Assuming Name is the property storing role names
                    .ToList();

                return new SelectList(roles);
            }
            catch (Exception ex)
            {
                // Handle exceptions (log, return empty list, etc.)
              
                return new SelectList(Enumerable.Empty<string>());
            }
        }

    }
}
