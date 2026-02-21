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
    await EnsureCategoriesTableAsync(dbContext);

    await EnsureRoleExistsAsync(roleManager, "Admin");
    await EnsureRoleExistsAsync(roleManager, "Librarian");
    await EnsureRoleExistsAsync(roleManager, "User");
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

static async Task EnsureCategoriesTableAsync(ApplicationDbContext dbContext)
{
    const string sql = """
        IF OBJECT_ID(N'[Categories]', N'U') IS NULL
        BEGIN
            CREATE TABLE [Categories] (
                [Id] INT IDENTITY(1,1) NOT NULL,
                [Name] NVARCHAR(100) NOT NULL,
                CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_Categories_Name] ON [Categories]([Name]);
        END;

        INSERT INTO [Categories]([Name])
        SELECT DISTINCT b.[CategoryName]
        FROM [Books] b
        WHERE b.[CategoryName] IS NOT NULL
          AND LTRIM(RTRIM(b.[CategoryName])) <> ''
          AND NOT EXISTS (
                SELECT 1
                FROM [Categories] c
                WHERE c.[Name] = b.[CategoryName]
          );
        """;

    await dbContext.Database.ExecuteSqlRawAsync(sql);
}
