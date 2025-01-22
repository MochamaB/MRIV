using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MRIV.Models;
using MRIV.Services;

var builder = WebApplication.CreateBuilder(args);

// Add all the DB Contexts Here. The connection strings in GetConnectionString() is in appsettings.json.

builder.Services.AddDbContext<KtdaleaveContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("KTDALeaveContext") ?? throw new InvalidOperationException("Connection string 'KTDALeaveContext' not found.")));
builder.Services.AddDbContext<RequisitionContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RequisitionContext") ?? throw new InvalidOperationException("Connection string 'KTDALeaveContext' not found.")));

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
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
