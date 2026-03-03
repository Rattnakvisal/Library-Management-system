using Library_Management_system.Data;
using Library_Management_system.Models;
using Library_Management_system.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Library_Management_system.Controllers.Admin;

[Authorize(Roles = "Admin,Librarian")]
[Route("admin/account")]
public class AdminAccountController : Controller
{
    private const string ProfileImageClaimType = "ProfileImageUrl";
    private const string CountryClaimType = "Country";
    private const string CityClaimType = "City";
    private const string AddressClaimType = "Address";
    private const string ZipCodeClaimType = "ZipCode";
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private readonly ApplicationDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AdminAccountController(
        ApplicationDbContext dbContext,
        IWebHostEnvironment environment,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _dbContext = dbContext;
        _environment = environment;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var (firstName, lastName) = SplitFullName(user.FullName);
        var profileClaims = await GetProfileClaimsAsync(user.Id);

        var model = new AdminProfileUpdateInput
        {
            FirstName = firstName,
            LastName = lastName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Country = profileClaims.GetValueOrDefault(CountryClaimType),
            City = profileClaims.GetValueOrDefault(CityClaimType),
            Address = profileClaims.GetValueOrDefault(AddressClaimType),
            ZipCode = profileClaims.GetValueOrDefault(ZipCodeClaimType)
        };

        await PopulateProfileMetaAsync(user);
        ViewBag.Title = "Profile";
        return View("~/Views/Admin/Account/Profile.cshtml", model);
    }

    [HttpPost("profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(AdminProfileUpdateInput input)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        input.FirstName = (input.FirstName ?? string.Empty).Trim();
        input.LastName = (input.LastName ?? string.Empty).Trim();
        input.Email = (input.Email ?? string.Empty).Trim();
        input.PhoneNumber = string.IsNullOrWhiteSpace(input.PhoneNumber)
            ? null
            : input.PhoneNumber.Trim();
        input.Country = string.IsNullOrWhiteSpace(input.Country) ? null : input.Country.Trim();
        input.City = string.IsNullOrWhiteSpace(input.City) ? null : input.City.Trim();
        input.Address = string.IsNullOrWhiteSpace(input.Address) ? null : input.Address.Trim();
        input.ZipCode = string.IsNullOrWhiteSpace(input.ZipCode) ? null : input.ZipCode.Trim();

        if (!ModelState.IsValid)
        {
            await PopulateProfileMetaAsync(user);
            ViewBag.Title = "Profile";
            return View("~/Views/Admin/Account/Profile.cshtml", input);
        }

        var existingUser = await _userManager.FindByEmailAsync(input.Email);
        if (existingUser != null && !string.Equals(existingUser.Id, user.Id, StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(input.Email), "Another account is already using this email.");
            await PopulateProfileMetaAsync(user);
            ViewBag.Title = "Profile";
            return View("~/Views/Admin/Account/Profile.cshtml", input);
        }

        user.FullName = $"{input.FirstName} {input.LastName}".Trim();
        user.Email = input.Email;
        user.PhoneNumber = input.PhoneNumber;
        user.EmailConfirmed = true;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await PopulateProfileMetaAsync(user);
            ViewBag.Title = "Profile";
            return View("~/Views/Admin/Account/Profile.cshtml", input);
        }

        await SaveProfileClaimsAsync(user.Id, input);
        await _signInManager.RefreshSignInAsync(user);
        TempData["AccountSuccess"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet("change-password")]
    public async Task<IActionResult> ChangePassword()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        await PopulateProfileMetaAsync(user);
        ViewBag.Title = "Change Password";
        return View("~/Views/Admin/Account/ChangePassword.cshtml", new AdminChangePasswordInput());
    }

    [HttpPost("change-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(AdminChangePasswordInput input)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            await PopulateProfileMetaAsync(user);
            ViewBag.Title = "Change Password";
            return View("~/Views/Admin/Account/ChangePassword.cshtml", input);
        }

        var changeResult = await _userManager.ChangePasswordAsync(
            user,
            input.CurrentPassword,
            input.NewPassword);

        if (!changeResult.Succeeded)
        {
            foreach (var error in changeResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await PopulateProfileMetaAsync(user);
            ViewBag.Title = "Change Password";
            return View("~/Views/Admin/Account/ChangePassword.cshtml", input);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["PasswordSuccess"] = "Password changed successfully.";
        return RedirectToAction(nameof(ChangePassword));
    }

    [HttpPost("profile/upload-image")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadProfileImage(IFormFile? imageFile)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var validationError = ValidateImageFile(imageFile);
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            TempData["AccountImageError"] = validationError;
            return RedirectToAction(nameof(Profile));
        }

        var imageUrl = await SaveProfileImageFileAsync(imageFile!);
        var existingClaimValue = await GetUserImageClaimValueAsync(user.Id);
        DeleteOwnedProfileImage(existingClaimValue);
        await UpsertUserImageClaimAsync(user, imageUrl);
        await _signInManager.RefreshSignInAsync(user);

        TempData["AccountImageSuccess"] = "Profile image updated successfully.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost("profile/remove-image")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveProfileImage()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var existingClaimValue = await GetUserImageClaimValueAsync(user.Id);
        DeleteOwnedProfileImage(existingClaimValue);

        var claims = await _userManager.GetClaimsAsync(user);
        var imageClaims = claims
            .Where(c => string.Equals(c.Type, ProfileImageClaimType, StringComparison.Ordinal))
            .ToList();

        foreach (var claim in imageClaims)
        {
            await _userManager.RemoveClaimAsync(user, claim);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["AccountImageSuccess"] = "Profile image removed successfully.";
        return RedirectToAction(nameof(Profile));
    }

    private async Task PopulateProfileMetaAsync(ApplicationUser user)
    {
        var displayName = string.IsNullOrWhiteSpace(user.FullName) ? user.UserName : user.FullName;
        var createdDateText = user.CreatedDate.HasValue
            ? user.CreatedDate.Value.ToLocalTime().ToString("MMM dd, yyyy")
            : "N/A";
        var profileImageUrl = await GetUserImageClaimValueAsync(user.Id) ?? string.Empty;
        var activeBorrowings = await _dbContext.BorrowingRecords
            .AsNoTracking()
            .CountAsync(x => x.ReturnDate == null);
        var overdueBorrowings = await _dbContext.BorrowingRecords
            .AsNoTracking()
            .CountAsync(x => x.ReturnDate == null && x.DueDate.Date < DateTime.UtcNow.Date);

        ViewBag.DisplayName = displayName;
        ViewBag.UserName = user.UserName ?? "N/A";
        ViewBag.MemberSince = createdDateText;
        ViewBag.EmailConfirmed = user.EmailConfirmed;
        ViewBag.ProfileImageUrl = profileImageUrl;
        ViewBag.TotalUsers = await _dbContext.Users.AsNoTracking().CountAsync();
        ViewBag.TotalBooks = await _dbContext.Books.AsNoTracking().CountAsync();
        ViewBag.ActiveBorrowings = activeBorrowings;
        ViewBag.OverdueBorrowings = overdueBorrowings;

        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            ViewBag.RoleText = "Admin";
            return;
        }

        if (await _userManager.IsInRoleAsync(user, "Librarian"))
        {
            ViewBag.RoleText = "Librarian";
            return;
        }

        ViewBag.RoleText = "Staff";
    }

    private static string? ValidateImageFile(IFormFile? imageFile)
    {
        if (imageFile is null || imageFile.Length == 0)
        {
            return "Please choose an image file.";
        }

        const long maxSizeBytes = 5 * 1024 * 1024;
        if (imageFile.Length > maxSizeBytes)
        {
            return "Image size must be 5 MB or smaller.";
        }

        var extension = Path.GetExtension(imageFile.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
        {
            return "Only JPG, JPEG, PNG, and WEBP files are allowed.";
        }

        if (string.IsNullOrWhiteSpace(imageFile.ContentType) ||
            !imageFile.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return "Invalid image file.";
        }

        return null;
    }

    private async Task<string> SaveProfileImageFileAsync(IFormFile imageFile)
    {
        var uploadsDirectory = Path.Combine(_environment.WebRootPath, "images", "Admin", "Profile", "uploads");
        Directory.CreateDirectory(uploadsDirectory);

        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadsDirectory, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await imageFile.CopyToAsync(stream);

        return $"/images/Admin/Profile/uploads/{fileName}";
    }

    private async Task<string?> GetUserImageClaimValueAsync(string userId)
    {
        return await _dbContext.UserClaims
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.ClaimType == ProfileImageClaimType)
            .OrderByDescending(x => x.Id)
            .Select(x => x.ClaimValue)
            .FirstOrDefaultAsync();
    }

    private async Task<Dictionary<string, string>> GetProfileClaimsAsync(string userId)
    {
        var claimTypes = new[] { CountryClaimType, CityClaimType, AddressClaimType, ZipCodeClaimType };
        var claims = await _dbContext.UserClaims
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.ClaimType != null && claimTypes.Contains(x.ClaimType))
            .OrderByDescending(x => x.Id)
            .ToListAsync();

        return claims
            .GroupBy(x => x.ClaimType!)
            .ToDictionary(
                g => g.Key,
                g => g.First().ClaimValue ?? string.Empty,
                StringComparer.Ordinal);
    }

    private async Task UpsertUserImageClaimAsync(ApplicationUser user, string claimValue)
    {
        var existingClaims = await _userManager.GetClaimsAsync(user);
        var existingClaim = existingClaims.FirstOrDefault(c => c.Type == ProfileImageClaimType);
        var newClaim = new Claim(ProfileImageClaimType, claimValue);

        if (existingClaim == null)
        {
            await _userManager.AddClaimAsync(user, newClaim);
            return;
        }

        if (existingClaim.Value == claimValue)
        {
            return;
        }

        await _userManager.ReplaceClaimAsync(user, existingClaim, newClaim);
    }

    private async Task SaveProfileClaimsAsync(string userId, AdminProfileUpdateInput input)
    {
        await UpsertOrRemoveProfileClaimAsync(userId, CountryClaimType, input.Country);
        await UpsertOrRemoveProfileClaimAsync(userId, CityClaimType, input.City);
        await UpsertOrRemoveProfileClaimAsync(userId, AddressClaimType, input.Address);
        await UpsertOrRemoveProfileClaimAsync(userId, ZipCodeClaimType, input.ZipCode);
    }

    private async Task UpsertOrRemoveProfileClaimAsync(string userId, string claimType, string? value)
    {
        var claims = await _dbContext.UserClaims
            .Where(x => x.UserId == userId && x.ClaimType == claimType)
            .OrderBy(x => x.Id)
            .ToListAsync();

        var normalizedValue = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalizedValue == null)
        {
            if (claims.Count > 0)
            {
                _dbContext.UserClaims.RemoveRange(claims);
                await _dbContext.SaveChangesAsync();
            }

            return;
        }

        if (claims.Count == 0)
        {
            _dbContext.UserClaims.Add(new IdentityUserClaim<string>
            {
                UserId = userId,
                ClaimType = claimType,
                ClaimValue = normalizedValue
            });
            await _dbContext.SaveChangesAsync();
            return;
        }

        claims[0].ClaimValue = normalizedValue;
        if (claims.Count > 1)
        {
            _dbContext.UserClaims.RemoveRange(claims.Skip(1));
        }

        await _dbContext.SaveChangesAsync();
    }

    private static (string FirstName, string LastName) SplitFullName(string? fullName)
    {
        var normalized = (fullName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return ("Admin", "User");
        }

        var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            return (parts[0], string.Empty);
        }

        return (parts[0], string.Join(" ", parts.Skip(1)));
    }

    private void DeleteOwnedProfileImage(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return;
        }

        var isOwnedAdminImage = imageUrl.StartsWith("/images/Admin/Profile/uploads/", StringComparison.OrdinalIgnoreCase);
        var isOwnedUserImage = imageUrl.StartsWith("/images/User/Profile/uploads/", StringComparison.OrdinalIgnoreCase);
        if (!isOwnedAdminImage && !isOwnedUserImage)
        {
            return;
        }

        var relativePath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var absolutePath = Path.Combine(_environment.WebRootPath, relativePath);

        if (System.IO.File.Exists(absolutePath))
        {
            System.IO.File.Delete(absolutePath);
        }
    }
}
