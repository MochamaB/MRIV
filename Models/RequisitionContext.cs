using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace MRIV.Models;

public partial class RequisitionContext : DbContext
{
    public RequisitionContext()
    {
    }

    public RequisitionContext(DbContextOptions<RequisitionContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Tbrequisition> Tbrequisitions { get; set; }
    public DbSet<Requisition> Requisitions { get; set; }
    public DbSet<RequisitionItem> RequisitionItems { get; set; }
    public DbSet<Material> Materials { get; set; }
    public DbSet<MaterialCategory> MaterialCategories { get; set; }

    public DbSet<MaterialCondition> MaterialCondition { get; set; }

    public DbSet<Approval> Approvals { get; set; }

    public DbSet<StationCategory> StationCategories { get; set; }
    public DbSet<WorkflowConfig> WorkflowConfigs { get; set; }
    public DbSet<WorkflowStepConfig> WorkflowStepConfigs { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }
    public virtual DbSet<NotificationTemplate> NotificationTemplates { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=.;Initial Catalog=requisition;Persist Security Info=True;User ID=sa;Password=P@ssw0rd;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>().ToTable("Department", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<Tbrequisition>().ToTable("tbrequisition", t => t.ExcludeFromMigrations());

        // Fluent API configurations (if needed)
        modelBuilder.Entity<Requisition>(entity =>
        {
            entity.HasMany(r => r.RequisitionItems)
                  .WithOne(ri => ri.Requisition)
                  .HasForeignKey(ri => ri.RequisitionId);

            entity.Property(r => r.Status)
                  .HasConversion<int>(); // Store enum as an integer

            entity.Ignore(r => r.Department);
        });

        modelBuilder.Entity<RequisitionItem>(entity =>
        {
           
            entity.Property(r => r.Status)
                 .HasConversion<int>(); // Store enum as an integer
        });

        modelBuilder.Entity<Approval>(entity =>
        {
            entity.HasOne(a => a.Requisition)
                  .WithMany(r => r.Approvals) // Make sure the back navigation property in Requisition is correctly set
                  .HasForeignKey(a => a.RequisitionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Ignore(r => r.Department);

            entity.Property(e => e.ApprovalStatus)
                  .HasConversion<int>(); // Store enum as an integer
        });

    
        modelBuilder.Entity<Material>()
            .HasOne(m => m.MaterialCategory)
            .WithMany(mc => mc.Materials)
            .HasForeignKey(m => m.MaterialCategoryId);

        OnModelCreatingPartial(modelBuilder);

        modelBuilder.Entity<WorkflowStepConfig>()
      .Property(e => e.Conditions)
      .HasColumnType("NVARCHAR(MAX)")
      .HasConversion(
          v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
          v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null));

        modelBuilder.Entity<WorkflowStepConfig>()
       .Property(e => e.RoleParameters)
       .HasColumnType("NVARCHAR(MAX)")
       .HasConversion(
           v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
           v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null));

        modelBuilder.Entity<StationCategory>().HasData(
      new StationCategory
      {
          Id = 1,
          Code = "headoffice",
          StationName = "Head Office",
          StationPoint = "both",
          DataSource = "Department",
          FilterCriteria = null // No special filtering needed for departments
      },
      new StationCategory
      {
          Id = 2,
          Code = "factory",
          StationName = "Factory",
          StationPoint = "both",
          DataSource = "Station",
          FilterCriteria = "{\"exclude\": [\"region\", \"zonal\"]}" // Exclude stations containing "region" or "zonal"
      },
      new StationCategory
      {
          Id = 3,
          Code = "region",
          StationName = "Region",
          StationPoint = "both",
          DataSource = "Station",
          FilterCriteria = "{\"include\": [\"region\"]}" // Only include stations with "region" in the name
      },
      new StationCategory
      {
          Id = 4,
          Code = "vendor",
          StationName = "Vendor",
          StationPoint = "delivery", // Vendors can only be delivery points, not issue points
          DataSource = "Vendor",
          FilterCriteria = null // No special filtering for vendors
      }
  );
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
