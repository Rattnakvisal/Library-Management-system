using Library_Management_system.Data;
using Library_Management_system.Models;
using Library_Management_system.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(30);
        }));



// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Change this from false to true
    options.SignIn.RequireConfirmedAccount = true; 
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

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login/Index";
    options.AccessDeniedPath = "/"; 
    
    // Explicitly handle the redirect to avoid the ReturnUrl parameter
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.Redirect("/");
        return Task.CompletedTask;
    };
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<DbHelper>();
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, Library_Management_system.Services.EmailSender>();
builder.Services.Configure<TelegramBotOptions>(builder.Configuration.GetSection(TelegramBotOptions.SectionName));
builder.Services.AddHttpClient<ITelegramNotifier, TelegramNotifier>();

var seedAdminEmail = builder.Configuration["SeedAdmin:Email"] ?? "admin@library.com";
var seedAdminPassword = builder.Configuration["SeedAdmin:Password"] ?? "Admin@123";
var resetSeedAdminPasswordOnStartup =
    builder.Configuration.GetValue("SeedAdmin:ResetPasswordOnStartup", builder.Environment.IsDevelopment());

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

    // Seed Admin User
    var adminUser = await userManager.FindByEmailAsync(seedAdminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = seedAdminEmail,
            Email = seedAdminEmail,
            FullName = "System Admin",
            EmailConfirmed = true
        };

        var createAdminResult = await userManager.CreateAsync(adminUser, seedAdminPassword);
        if (!createAdminResult.Succeeded)
        {
            var errors = string.Join("; ", createAdminResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create admin user '{seedAdminEmail}': {errors}");
        }
    }

    if (adminUser == null)
    {
        throw new InvalidOperationException($"Seed admin user '{seedAdminEmail}' could not be loaded.");
    }

    if (!adminUser.EmailConfirmed)
    {
        adminUser.EmailConfirmed = true;
        var confirmEmailResult = await userManager.UpdateAsync(adminUser);
        if (!confirmEmailResult.Succeeded)
        {
            var errors = string.Join("; ", confirmEmailResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to confirm seed admin email '{seedAdminEmail}': {errors}");
        }
    }

    if (resetSeedAdminPasswordOnStartup)
    {
        var hasExpectedPassword = await userManager.CheckPasswordAsync(adminUser, seedAdminPassword);
        if (!hasExpectedPassword)
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(adminUser);
            var resetPasswordResult = await userManager.ResetPasswordAsync(adminUser, resetToken, seedAdminPassword);
            if (!resetPasswordResult.Succeeded)
            {
                var errors = string.Join("; ", resetPasswordResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to reset seed admin password '{seedAdminEmail}': {errors}");
            }
        }
    }

    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        var addRoleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
        if (!addRoleResult.Succeeded)
        {
            var errors = string.Join("; ", addRoleResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to assign Admin role to '{seedAdminEmail}': {errors}");
        }
    }
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
