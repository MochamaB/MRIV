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
                            StepAction = "Approve",
                            ApproverRole = "supervisor",
                            RoleParameters = new Dictionary<string, string>
                            {
                                { "roles", "Hod,supervisor,Admin,HR" } // Multiple allowed roles
                                         // Additional filter parameter
                            },
                            Conditions = new Dictionary<string, string>
                            {
                                { "restrictToPayroll", "true" } // Only one with payroll can view
                                         // Additional filter parameter
                            }
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 2,
                            StepName = "HO Employee Receipt",
                            StepAction = "Receive",
                            ApproverRole = "user",
                            RoleParameters = new Dictionary<string, string>
                            {
                                { "roles", "Hod,supervisor,Admin,HR,user" } // Multiple allowed roles
                                // Additional filter parameter
                            }
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
                            StepAction = "Approve",
                            ApproverRole = "supervisor",
                            RoleParameters = new Dictionary<string, string>
                            {
                                { "roles", "Hod,supervisor,Admin,HR" } // Multiple allowed roles
                                // Additional filter parameter
                            },
                            Conditions = new Dictionary<string, string>
                            {
                                { "restrictToPayroll", "true" } // Only one with payroll can view
                                         // Additional filter parameter
                            }
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 2,
                            StepName = "Admin Dispatch Approval",
                            StepAction = "Dispatch",
                            ApproverRole = "dispatchAdmin",
                             RoleParameters = new Dictionary<string, string>
                            {
                                { "roles", "Hod,supervisor,Admin,HR" } // Multiple allowed roles
                                // Additional filter parameter
                            },
                            Conditions = new Dictionary<string, string>
                            {
                                { "dispatchType", "admin" }
                            }
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 2,
                            StepName = "Vendor Dispatch",
                            StepAction = "Dispatch",
                            ApproverRole = "vendor",
                            RoleParameters = new Dictionary<string, string>
                             {
                                 { "roles", "Hod,supervisor,Admin,HR,user" } // Multiple allowed roles
                                 // Additional filter parameter
                             },
                            Conditions = new Dictionary<string, string>
                            {
                                { "dispatchType", "vendor" }
                            }
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 3,
                            StepName = "Factory Employee Receipt",
                            StepAction = "Receive",
                            ApproverRole = "FieldUser",
                            RoleParameters = new Dictionary<string, string>
                            { 
                                { "roles", "Hod,supervisor,Admin,FieldSupervisor" }
                            }
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
                            StepAction = "Approve",
                            ApproverRole = "supervisor",
                            RoleParameters = new Dictionary<string, string>
                            { { "roles", "Hod,supervisor,Admin,FieldSupervisor" } },
                            Conditions = new Dictionary<string, string>
                            {
                                { "restrictToPayroll", "true" } // Only one with payroll can view
                                         // Additional filter parameter
                            }
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 2,
                            StepName = "Admin Dispatch Approval",
                            StepAction = "Approve",
                            ApproverRole = "dispatchAdmin",
                            RoleParameters = new Dictionary<string, string>
                             {
                                 { "roles", "Hod,supervisor,Admin,HR,user" } // Multiple allowed roles
                                 
                             },
                            Conditions = new Dictionary<string, string>
                            {
                                { "dispatchType", "admin" }
                            }
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 2,
                            StepName = "Vendor Dispatch",
                            StepAction = "Dispatch",
                            ApproverRole = "vendor",
                            RoleParameters = new Dictionary<string, string>
                             {
                                 { "roles", "Hod,supervisor,Admin,HR,user" } // Multiple allowed roles
                                 
                             },
                            Conditions = new Dictionary<string, string>
                            {
                                { "dispatchType", "vendor" }
                            }
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 3,
                            StepName = "Region Employee Receipt",
                            StepAction = "Receive",
                            ApproverRole = "FieldSupervisor",
                            RoleParameters = new Dictionary<string, string>
                             {
                                { "roles", "Hod,supervisor,Admin,FieldSupervisor" }
                            }
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
                            StepAction = "Approve",
                            ApproverRole = "supervisor",
                            RoleParameters = new Dictionary<string, string>
                            { { "roles", "Hod,supervisor,Admin,FieldSupervisor,HR" }, { "station", "HQ" } }
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 2,
                            StepName = "Admin Dispatch Approval",
                            StepAction = "Approve",
                            ApproverRole = "dispatchAdmin",
                            RoleParameters = new Dictionary<string, string>
                             {
                                 { "roles", "Hod,supervisor,Admin,HR,user" } // Multiple allowed roles
                                  // Additional filter parameter
                             },
                            Conditions = new Dictionary<string, string>
                            {
                                { "dispatchType", "admin" }
                            }
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 2,
                            StepName = "Vendor Dispatch",
                            StepAction = "Dispatch",
                            ApproverRole = "vendor",
                            RoleParameters = new Dictionary<string, string>
                             {
                                 { "roles", "Hod,supervisor,Admin,HR,user" } // Multiple allowed roles
                                
                             },
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
                            StepAction = "Approve",
                            ApproverRole = "supervisor",
                            RoleParameters = new Dictionary<string, string>
                            { { "roles", "Hod,supervisor,Admin,FieldSupervisor" } }
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 2,
                            StepName = "HO Employee Receipt",
                            StepAction = "Receive",
                            ApproverRole = "user",
                            RoleParameters = new Dictionary<string, string>
                            {
                                { "roles", "Hod,supervisor,Admin,HR,user" } // Multiple allowed roles
                               
                            }
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
                            StepAction = "Approve",
                            ApproverRole = "supervisor",
                            RoleParameters = new Dictionary<string, string>
                            {
                                { "roles", "Hod,supervisor,Admin,FieldSupervisor,FieldUser" } // Multiple allowed roles
                               
                            }
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 2,
                            StepName = "Factory Employee Receipt",
                            StepAction = "Receive",
                            ApproverRole = "FieldUser",
                            RoleParameters = new Dictionary<string, string>
                            {
                                { "roles", "Hod,supervisor,Admin,FieldSupervisor,FieldUser" }// Multiple allowed roles
                           
                            }
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
                            StepAction = "Approve",
                            ApproverRole = "supervisor",
                            RoleParameters = new Dictionary<string, string>
                             { { "roles", "Hod,supervisor,Admin,FieldSupervisor" } }
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 2,
                            StepName = "Region Employee Receipt",
                            StepAction = "Receive",
                            ApproverRole = "FieldSupervisor",
                            RoleParameters = new Dictionary<string, string>
                            { { "roles", "Hod,supervisor,Admin,FieldSupervisor,FieldUser" } }
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
                            StepAction = "Approve",
                            ApproverRole = "supervisor",
                            RoleParameters = new Dictionary<string, string>
                             { { "roles", "Hod,supervisor,Admin,FieldSupervisor,FieldUser" } }
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
                            StepAction = "Approve",
                            ApproverRole = "supervisor",
                            RoleParameters = new Dictionary<string, string>
                             { { "roles", "Hod,supervisor,Admin,FieldSupervisor" } }
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 2,
                            StepName = "HO Employee Receipt",
                            StepAction = "Receive",
                            ApproverRole = "user",
                            RoleParameters = new Dictionary<string, string>
                             { { "roles", "Hod,supervisor,Admin,FieldSupervisor" } }
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
                            StepAction = "Approve",
                            ApproverRole = "supervisor",
                            RoleParameters = new Dictionary<string, string>
                             { { "roles", "Hod,supervisor,Admin,FieldSupervisor" } }
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 2,
                            StepName = "Factory Employee Receipt",
                            StepAction = "Receive",
                            ApproverRole = "FieldUser",
                            RoleParameters = new Dictionary<string, string>
                             { { "roles", "Hod,supervisor,Admin,FieldSupervisor,FieldUser" } }
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
                            StepAction = "Approve",
                            ApproverRole = "supervisor",
                            RoleParameters = new Dictionary<string, string>
                             { { "roles", "Hod,supervisor,Admin,FieldSupervisor" } }
                        },
                        new WorkflowStepConfig
                        {
                            StepOrder = 2,
                            StepName = "Region Employee Receipt",
                            StepAction = "Receive",
                            ApproverRole = "FieldSupervisor",
                            RoleParameters = new Dictionary<string, string>
                             { { "roles", "Hod,supervisor,Admin,FieldSupervisor,FieldUser" } }
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
                            StepAction = "Approve",
                            ApproverRole = "supervisor",
                            RoleParameters = new Dictionary<string, string>
                            { { "roles", "Hod,supervisor,Admin,FieldSupervisor" } }
                        }
                    }
                }
            };

            context.AddRange(workflowConfigs);
            context.SaveChanges();
        }
    }
}