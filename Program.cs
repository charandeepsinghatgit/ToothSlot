using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ToothSlot.Data;
using ToothSlot.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21))));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddScoped<IUserStore<ApplicationUser>, UserStore<ApplicationUser, IdentityRole, ApplicationDbContext>>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Auto-migrate and seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    
    // Run migrations
    context.Database.Migrate();
    
    // Seed roles
    string[] roleNames = { "Admin", "Dentist", "Patient" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
    
    // Seed Admin
    if (await userManager.FindByEmailAsync("admin@toothslot.com") == null)
    {
        var admin = new ApplicationUser
        {
            UserName = "admin@toothslot.com",
            Email = "admin@toothslot.com",
            FirstName = "Admin",
            LastName = "User",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(admin, "Password@123");
        await userManager.AddToRoleAsync(admin, "Admin");
    }
    
    // Seed Dentist
    if (await userManager.FindByEmailAsync("dr.smith@toothslot.com") == null)
    {
        var dentist = new ApplicationUser
        {
            UserName = "dr.smith@toothslot.com",
            Email = "dr.smith@toothslot.com",
            FirstName = "John",
            LastName = "Smith",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(dentist, "Password@123");
        await userManager.AddToRoleAsync(dentist, "Dentist");
        
        // Create dentist profile (only with fields that exist)
        context.DentistProfiles.Add(new DentistProfile
        {
            UserId = dentist.Id,
            Specialization = "General Dentistry"
        });
    }
    
    // Seed Patient
    if (await userManager.FindByEmailAsync("patient@test.com") == null)
    {
        var patient = new ApplicationUser
        {
            UserName = "patient@test.com",
            Email = "patient@test.com",
            FirstName = "Jane",
            LastName = "Doe",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(patient, "Password@123");
        await userManager.AddToRoleAsync(patient, "Patient");
    }
    
    // Seed Services
    if (!context.DentalServices.Any())
    {
        context.DentalServices.AddRange(
            new DentalService { Name = "General Checkup", Description = "Routine dental examination", Price = 50.00m, DurationMinutes = 30 },
            new DentalService { Name = "Teeth Cleaning", Description = "Professional teeth cleaning", Price = 75.00m, DurationMinutes = 45 },
            new DentalService { Name = "Tooth Extraction", Description = "Removal of damaged tooth", Price = 150.00m, DurationMinutes = 60 },
            new DentalService { Name = "Cavity Filling", Description = "Dental filling procedure", Price = 100.00m, DurationMinutes = 45 },
            new DentalService { Name = "Root Canal", Description = "Root canal treatment", Price = 500.00m, DurationMinutes = 90 }
        );
    }
    
    await context.SaveChangesAsync();
}

// Auto-assign Patient role to users without roles
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    
    var allUsers = userManager.Users.ToList();
    foreach (var user in allUsers)
    {
        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Any())
        {
            await userManager.AddToRoleAsync(user, "Patient");
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();