using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MRIV.Models;
using MRIV.Services;
using MRIV.Data;
using MRIV.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add all the DB Contexts Here. The connection strings in GetConnectionString() is in appsettings.json.

builder.Services.AddDbContext<KtdaleaveContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("KTDALeaveContext") ?? throw new InvalidOperationException("Connection string 'KTDALeaveContext' not found."),
    sqlServerOptionsAction: sqlOptions =>
    {
        // Configure explicit connection pooling
        sqlOptions.MaxBatchSize(100);
        sqlOptions.CommandTimeout(90);
        
        // Add connection resiliency with retry logic
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    }));
builder.Services.AddDbContext<RequisitionContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RequisitionContext") ?? throw new InvalidOperationException("Connection string 'RequisitionContext' not found."),
    sqlServerOptionsAction: sqlOptions =>
    {
        // Configure explicit connection pooling
        sqlOptions.MaxBatchSize(100);
        sqlOptions.CommandTimeout(90);
        
        // Add connection resiliency with retry logic
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    }));

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<IVisibilityAuthorizeService, VisibilityAuthorizeService>();
builder.Services.AddScoped<IStationCategoryService, StationCategoryService>();
builder.Services.AddScoped<VendorService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<ILocationService, LocationService>();

// Add settings service
builder.Services.AddSettingsService();

builder.Services.AddHttpContextAccessor();

// Add session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout as needed
    options.Cookie.HttpOnly = true; // Make the session cookie accessible only to the server
    options.Cookie.IsEssential = true; // Make the session cookie essential
});
builder.Services.AddHttpContextAccessor();

var app = builder.Build();
// Apply migrations and seed data in development

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var ktdaContext = services.GetRequiredService<KtdaleaveContext>();
    try
    {
        var context = services.GetRequiredService<RequisitionContext>();
        WorkflowConfigSeeder.SeedWorkflowConfigurations(context);

        // Seed notification templates (new seeder)
        NotificationTemplateSeeder.SeedNotificationTemplates(context);
        
        // Seed settings
        SettingsSeeder.SeedSettings(context);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while seeding the database: {ex.Message}");
    }

}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
// Add session middleware
app.UseSession();

app.UseRouting();

app.UseAuthorization();




app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();
