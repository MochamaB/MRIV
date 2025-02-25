using MRIV.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace MRIV.Data
{
    public static class WorkflowConfigSeeder
    {
        public static void SeedWorkflowConfigurations(RequisitionContext context)
        {
            if (context.Set<WorkflowConfig>().Any())
            {
                return; // DB has already been seeded
            }

            // Define all the workflow configurations based on the previous dictionary
            var workflowConfigs = new List<WorkflowConfig>
            {
                // HO -> HO
                new WorkflowConfig
                {
                    IssueStationCategory = "headoffice",
                    DeliveryStationCategory = "headoffice",
                    Steps = new List<WorkflowStepConfig>
                    {
                        new WorkflowStepConfig
                        {
                            StepOrder = 1,
                            StepName = "Supervisor Approval",
                            ApproverRole = "supervisor",
                            RoleParameters = new Dictionary<string, string>()
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 2,
                            StepName = "HO Employee Receipt",
                            ApproverRole = "user",
                            RoleParameters = new Dictionary<string, string>()
                        }
                    }
                },
                
                // HO -> Factory
                new WorkflowConfig
                {
                    IssueStationCategory = "headoffice",
                    DeliveryStationCategory = "factory",
                    Steps = new List<WorkflowStepConfig>
                    {
                        new WorkflowStepConfig
                        {
                            StepOrder = 1,
                            StepName = "Supervisor Approval",
                            ApproverRole = "supervisor",
                            RoleParameters = new Dictionary<string, string>()
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 2,
                            StepName = "Admin Dispatch Approval",
                            ApproverRole = "dispatchAdmin",
                            Conditions = new Dictionary<string, string>
                            {
                                { "dispatchType", "admin" }
                            }
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 2,
                            StepName = "Vendor Dispatch",
                            ApproverRole = "vendor",
                            Conditions = new Dictionary<string, string>
                            {
                                { "dispatchType", "vendor" }
                            }
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 3,
                            StepName = "Factory Employee Receipt",
                            ApproverRole = "FieldUser",
                            RoleParameters = new Dictionary<string, string>()
                        }
                    }
                },
                
                // HO -> Region
                new WorkflowConfig
                {
                    IssueStationCategory = "headoffice",
                    DeliveryStationCategory = "region",
                    Steps = new List<WorkflowStepConfig>
                    {
                        new WorkflowStepConfig
                        {
                            StepOrder = 1,
                            StepName = "Supervisor Approval",
                            ApproverRole = "supervisor",
                            RoleParameters = new Dictionary<string, string>()
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 2,
                            StepName = "Admin Dispatch Approval",
                            ApproverRole = "dispatchAdmin",
                            Conditions = new Dictionary<string, string>
                            {
                                { "dispatchType", "admin" }
                            }
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 2,
                            StepName = "Vendor Dispatch",
                            ApproverRole = "vendor",
                            Conditions = new Dictionary<string, string>
                            {
                                { "dispatchType", "vendor" }
                            }
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 3,
                            StepName = "Region Employee Receipt",
                            ApproverRole = "FieldSupervisor",
                            RoleParameters = new Dictionary<string, string>()
                        }
                    }
                },
                
                // HO -> Vendor
                new WorkflowConfig
                {
                    IssueStationCategory = "headoffice",
                    DeliveryStationCategory = "vendor",
                    Steps = new List<WorkflowStepConfig>
                    {
                        new WorkflowStepConfig
                        {
                            StepOrder = 1,
                            StepName = "Supervisor Approval",
                            ApproverRole = "supervisor",
                            RoleParameters = new Dictionary<string, string>()
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 2,
                            StepName = "Admin Dispatch Approval",
                            ApproverRole = "dispatchAdmin",
                            Conditions = new Dictionary<string, string>
                            {
                                { "dispatchType", "admin" }
                            }
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 2,
                            StepName = "Vendor Dispatch",
                            ApproverRole = "vendor",
                            Conditions = new Dictionary<string, string>
                            {
                                { "dispatchType", "vendor" }
                            }
                        }
                    }
                },
                
                // Factory -> HO
                new WorkflowConfig
                {
                    IssueStationCategory = "factory",
                    DeliveryStationCategory = "headoffice",
                    Steps = new List<WorkflowStepConfig>
                    {
                        new WorkflowStepConfig
                        {
                            StepOrder = 1,
                            StepName = "Supervisor Approval",
                            ApproverRole = "supervisor",
                            RoleParameters = new Dictionary<string, string>()
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 2,
                            StepName = "HO Employee Receipt",
                            ApproverRole = "user",
                            RoleParameters = new Dictionary<string, string>()
                        }
                    }
                },
                
                // Factory -> Factory
                new WorkflowConfig
                {
                    IssueStationCategory = "factory",
                    DeliveryStationCategory = "factory",
                    Steps = new List<WorkflowStepConfig>
                    {
                        new WorkflowStepConfig
                        {
                            StepOrder = 1,
                            StepName = "Supervisor Approval",
                            ApproverRole = "supervisor",
                            RoleParameters = new Dictionary<string, string>()
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 2,
                            StepName = "Factory Employee Receipt",
                            ApproverRole = "FieldUser",
                            RoleParameters = new Dictionary<string, string>()
                        }
                    }
                },
                
                // Factory -> Region
                new WorkflowConfig
                {
                    IssueStationCategory = "factory",
                    DeliveryStationCategory = "region",
                    Steps = new List<WorkflowStepConfig>
                    {
                        new WorkflowStepConfig
                        {
                            StepOrder = 1,
                            StepName = "Supervisor Approval",
                            ApproverRole = "supervisor",
                            RoleParameters = new Dictionary<string, string>()
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 2,
                            StepName = "Region Employee Receipt",
                            ApproverRole = "FieldSupervisor",
                            RoleParameters = new Dictionary<string, string>()
                        }
                    }
                },
                
                // Factory -> Vendor
                new WorkflowConfig
                {
                    IssueStationCategory = "factory",
                    DeliveryStationCategory = "vendor",
                    Steps = new List<WorkflowStepConfig>
                    {
                        new WorkflowStepConfig
                        {
                            StepOrder = 1,
                            StepName = "Supervisor Approval",
                            ApproverRole = "supervisor",
                            RoleParameters = new Dictionary<string, string>()
                        }
                    }
                },
                
                // Region -> HO
                new WorkflowConfig
                {
                    IssueStationCategory = "region",
                    DeliveryStationCategory = "headoffice",
                    Steps = new List<WorkflowStepConfig>
                    {
                        new WorkflowStepConfig
                        {
                            StepOrder = 1,
                            StepName = "Supervisor Approval",
                            ApproverRole = "supervisor",
                            RoleParameters = new Dictionary<string, string>()
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 2,
                            StepName = "HO Employee Receipt",
                            ApproverRole = "user",
                            RoleParameters = new Dictionary<string, string>()
                        }
                    }
                },
                
                // Region -> Factory
                new WorkflowConfig
                {
                    IssueStationCategory = "region",
                    DeliveryStationCategory = "factory",
                    Steps = new List<WorkflowStepConfig>
                    {
                        new WorkflowStepConfig
                        {
                            StepOrder = 1,
                            StepName = "Supervisor Approval",
                            ApproverRole = "supervisor",
                            RoleParameters = new Dictionary<string, string>()
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 2,
                            StepName = "Factory Employee Receipt",
                            ApproverRole = "FieldUser",
                            RoleParameters = new Dictionary<string, string>()
                        }
                    }
                },
                
                // Region -> Region
                new WorkflowConfig
                {
                    IssueStationCategory = "region",
                    DeliveryStationCategory = "region",
                    Steps = new List<WorkflowStepConfig>
                    {
                        new WorkflowStepConfig
                        {
                            StepOrder = 1,
                            StepName = "Supervisor Approval",
                            ApproverRole = "supervisor",
                            RoleParameters = new Dictionary<string, string>()
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 2,
                            StepName = "Region Employee Receipt",
                            ApproverRole = "FieldSupervisor",
                            RoleParameters = new Dictionary<string, string>()
                        }
                    }
                },
                
                // Region -> Vendor
                new WorkflowConfig
                {
                    IssueStationCategory = "region",
                    DeliveryStationCategory = "vendor",
                    Steps = new List<WorkflowStepConfig>
                    {
                        new WorkflowStepConfig
                        {
                            StepOrder = 1,
                            StepName = "Supervisor Approval",
                            ApproverRole = "supervisor",
                            RoleParameters = new Dictionary<string, string>()
                        }
                    }
                }
            };

            context.AddRange(workflowConfigs);
            context.SaveChanges();
        }
    }
}