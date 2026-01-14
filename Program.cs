using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Data;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// Set license contexts
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add Identity
builder.Services.AddIdentity<Microsoft.AspNetCore.Identity.IdentityUser, Microsoft.AspNetCore.Identity.IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
});

var app = builder.Build();

// Log the active connection string so we can confirm which database is used at runtime
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("DefaultConnection at runtime: {Connection}", connectionString);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
// Enable authentication middleware so Identity cookies are processed
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// Explicit routes for publicly browsable pages (ensure these paths resolve)
app.MapControllerRoute(
    name: "inventory",
    pattern: "Inventory/{action=Index}/{id?}",
    defaults: new { controller = "Inventory", action = "Index" });

app.MapControllerRoute(
    name: "about",
    pattern: "About/{action=Index}/{id?}",
    defaults: new { controller = "About", action = "Index" });

app.MapControllerRoute(
    name: "contact",
    pattern: "Contact/{action=Index}/{id?}",
    defaults: new { controller = "Contact", action = "Index" });

app.MapRazorPages();

app.Run();

