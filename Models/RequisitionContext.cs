using System;
using System.Collections.Generic;
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
    public DbSet<Approval> Approvals { get; set; }

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
        });

    
        modelBuilder.Entity<Material>()
            .HasOne(m => m.MaterialCategory)
            .WithMany(mc => mc.Materials)
            .HasForeignKey(m => m.MaterialCategoryId);

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
