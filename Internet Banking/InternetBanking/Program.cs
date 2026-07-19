using InternetBanking.Core.Application;
using InternetBanking.Infrastructure.Identity;
using InternetBanking.Infrastructure.Identity.Entities;
using InternetBanking.Infrastructure.Identity.Seeds;
using InternetBanking.Infrastructure.Persistence;
using InternetBanking.Infrastructure.Persistence.Seeds;
using InternetBanking.WebApp.Middlewares;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
builder.Services.AddPersistenceInfrastructure(builder.Configuration);
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddApplicationLayer();
builder.Services.AddScoped<LoginAuthorize>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ValidateUserSession>();

var app = builder.Build();

if (app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("DemoSeed:Enabled"))
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("StartupSeeder");

    try
    {
        string adminPassword = builder.Configuration["DemoSeed:AdminPassword"] ?? string.Empty;
        string clientPassword = builder.Configuration["DemoSeed:ClientPassword"] ?? string.Empty;
        string secondaryClientPassword = builder.Configuration["DemoSeed:SecondaryClientPassword"] ?? string.Empty;

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var applicationContext = services.GetRequiredService<InternetBanking.Infrastructure.Persistence.Contexts.ApplicationContext>();

        await DefaultRoles.SeedAsync(userManager, roleManager);
        await DefaultAdminUser.SeedAsync(userManager, roleManager, adminPassword);
        await DefaultClientUser.SeedAsync(userManager, roleManager, clientPassword);
        await DemoDataSeeder.SeedAsync(applicationContext, userManager, secondaryClientPassword);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Development demo data could not be seeded. No passwords or connection strings were logged.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
