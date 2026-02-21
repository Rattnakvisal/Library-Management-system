using Library_Management_system.Data;
using Library_Management_system.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;

    // Optional: password rules (you can adjust)
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddScoped<DbHelper>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error/500");
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

await using (var scope = app.Services.CreateAsyncScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    await dbContext.Database.MigrateAsync();

    await EnsureRoleExistsAsync(roleManager, "Admin");
    await EnsureRoleExistsAsync(roleManager, "Librarian");
    await EnsureRoleExistsAsync(roleManager, "User");

    await EnsureUserInRoleAsync(
        userManager,
        email: "admin@library.com",
        userName: "SystemAdmin",
        fullName: "System Admin",
        password: "Admin@12345",
        role: "Admin");

    await EnsureUserInRoleAsync(
        userManager,
        email: "librarian@library.com",
        userName: "SystemLibrarian",
        fullName: "System Librarian",
        password: "Librarian@12345",
        role: "Librarian");
}

app.Run();

static async Task EnsureRoleExistsAsync(RoleManager<IdentityRole> roleManager, string roleName)
{
    if (await roleManager.RoleExistsAsync(roleName))
    {
        return;
    }

    var createRoleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
    if (!createRoleResult.Succeeded)
    {
        var errors = string.Join("; ", createRoleResult.Errors.Select(e => e.Description));
        throw new InvalidOperationException($"Failed to create role '{roleName}': {errors}");
    }
}

static async Task EnsureUserInRoleAsync(
    UserManager<ApplicationUser> userManager,
    string email,
    string userName,
    string fullName,
    string password,
    string role)
{
    var user = await userManager.FindByEmailAsync(email);
    if (user == null)
    {
        user = new ApplicationUser
        {
            Email = email,
            UserName = userName,
            FullName = fullName,
            EmailConfirmed = true
        };

        var createUserResult = await userManager.CreateAsync(user, password);
        if (!createUserResult.Succeeded)
        {
            var errors = string.Join("; ", createUserResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user '{email}': {errors}");
        }
    }

    if (!await userManager.IsInRoleAsync(user, role))
    {
        var addToRoleResult = await userManager.AddToRoleAsync(user, role);
        if (!addToRoleResult.Succeeded)
        {
            var errors = string.Join("; ", addToRoleResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to add user '{email}' to role '{role}': {errors}");
        }
    }
}
