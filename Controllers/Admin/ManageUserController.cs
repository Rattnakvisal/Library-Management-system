using System.Security.Claims;
using System.Text.RegularExpressions;
using Library_Management_system.Data;
using Library_Management_system.Models;
using Library_Management_system.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_system.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("admin/manageuser")]
public class ManageUserController : Controller
{
    private const string StudentRole = "User";
    private const string AdminRole = "Admin";
    private const string LibrarianRole = "Librarian";
    private const string GenderClaimType = "Gender";
    private const string UserCodeClaimType = "UserCode";

    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public ManageUserController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? tab, string? search, string? gender, string? roleFilter, string? sort)
    {
        var normalizedTab = NormalizeTab(tab);
        var searchText = (search ?? string.Empty).Trim();
        var normalizedGenderFilter = NormalizeGenderFilter(gender);
        var normalizedRoleFilter = NormalizeRoleFilter(roleFilter);
        var normalizedSort = NormalizeSort(sort);

        var users = await _dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.FullName)
            .ThenBy(u => u.Email)
            .ToListAsync();

        var userIds = users.Select(u => u.Id).ToArray();

        var rolePairs = await (
                from ur in _dbContext.UserRoles
                join role in _dbContext.Roles on ur.RoleId equals role.Id
                where userIds.Contains(ur.UserId)
                select new { ur.UserId, RoleName = role.Name }
            )
            .ToListAsync();

        var claims = await _dbContext.UserClaims
            .AsNoTracking()
            .Where(c => userIds.Contains(c.UserId)
                        && (c.ClaimType == GenderClaimType || c.ClaimType == UserCodeClaimType))
            .ToListAsync();

        var rolesByUser = rolePairs
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName ?? string.Empty).Where(x => x.Length > 0).ToList());

        var genderByUser = claims
            .Where(c => c.ClaimType == GenderClaimType)
            .GroupBy(c => c.UserId)
            .ToDictionary(g => g.Key, g => g.Last().ClaimValue ?? string.Empty);

        var codeByUser = claims
            .Where(c => c.ClaimType == UserCodeClaimType)
            .GroupBy(c => c.UserId)
            .ToDictionary(g => g.Key, g => g.Last().ClaimValue ?? string.Empty);

        var items = users.Select(user =>
        {
            var roles = rolesByUser.TryGetValue(user.Id, out var mappedRoles) ? mappedRoles : new List<string>();
            var role = ResolveRole(roles);
            var isStaff = IsStaffRole(role);
            var rawGender = genderByUser.TryGetValue(user.Id, out var gender) ? gender : string.Empty;
            var rawCode = codeByUser.TryGetValue(user.Id, out var userCode) ? userCode : string.Empty;

            return new ManageUserItemViewModel
            {
                UserId = user.Id,
                UserCode = NormalizeUserCode(rawCode, user.Id),
                FullName = string.IsNullOrWhiteSpace(user.FullName) ? user.UserName ?? "Unknown" : user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Gender = NormalizeGender(rawGender),
                Role = role == StudentRole ? "Student" : role,
                IsStaff = isStaff
            };
        });

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            items = items.Where(item =>
                ContainsIgnoreCase(item.UserCode, searchText) ||
                ContainsIgnoreCase(item.FullName, searchText) ||
                ContainsIgnoreCase(item.Email, searchText) ||
                ContainsIgnoreCase(item.PhoneNumber, searchText) ||
                ContainsIgnoreCase(item.Gender, searchText) ||
                ContainsIgnoreCase(item.Role, searchText));
        }

        if (!string.IsNullOrWhiteSpace(normalizedGenderFilter))
        {
            var displayGender = GetDisplayGenderForFilter(normalizedGenderFilter);
            items = items.Where(item => string.Equals(item.Gender, displayGender, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(normalizedRoleFilter))
        {
            items = items.Where(item => IsRoleMatched(item.Role, normalizedRoleFilter));
        }

        var itemList = items.ToList();

        var students = ApplySort(
                itemList.Where(x => !x.IsStaff),
                normalizedSort
            )
            .ToList();

        var staffs = ApplySort(
                itemList.Where(x => x.IsStaff),
                normalizedSort
            )
            .ToList();

        var totalStudents = users.Count(u =>
        {
            var userRoles = rolesByUser.TryGetValue(u.Id, out var mappedRoles) ? mappedRoles : new List<string>();
            return !IsStaffRole(ResolveRole(userRoles));
        });

        var totalStaff = users.Count - totalStudents;

        var model = new ManageUserPageViewModel
        {
            TotalStudents = totalStudents,
            TotalStaff = totalStaff,
            TotalUsers = users.Count,
            ActiveTab = normalizedTab,
            Search = searchText,
            GenderFilter = normalizedGenderFilter,
            RoleFilter = normalizedRoleFilter,
            Sort = normalizedSort,
            Students = students,
            Staffs = staffs
        };

        return View("~/Views/Admin/ManageUser/Index.cshtml", model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ManageUserFormInput input)
    {
        var role = NormalizeRole(input.Role);
        if (!IsSupportedRole(role))
        {
            TempData["ManageUserError"] = "Invalid role selected.";
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort));
        }

        if (!ModelState.IsValid)
        {
            TempData["ManageUserError"] = BuildModelStateErrorMessage();
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort));
        }

        if (await _userManager.FindByEmailAsync(input.Email.Trim()) != null)
        {
            TempData["ManageUserError"] = "A user with this email already exists.";
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort));
        }

        var user = new ApplicationUser
        {
            FullName = input.FullName.Trim(),
            Email = input.Email.Trim(),
            EmailConfirmed = true,
            PhoneNumber = input.PhoneNumber.Trim(),
            UserName = await GenerateUniqueUsernameAsync(input.FullName, input.Email)
        };

        var createResult = await _userManager.CreateAsync(user, input.Password.Trim());
        if (!createResult.Succeeded)
        {
            TempData["ManageUserError"] = BuildIdentityErrorMessage(createResult);
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            TempData["ManageUserError"] = BuildIdentityErrorMessage(roleResult);
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort));
        }

        await UpsertUserClaimsAsync(user, input.UserCode, input.Gender);

        TempData["ManageUserSuccess"] = "User created successfully.";
        return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort));
    }

    [HttpPost("update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ManageUserUpdateInput input)
    {
        var role = NormalizeRole(input.Role);
        if (!IsSupportedRole(role))
        {
            TempData["ManageUserError"] = "Invalid role selected.";
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort));
        }

        if (!ModelState.IsValid)
        {
            TempData["ManageUserError"] = BuildModelStateErrorMessage();
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort));
        }

        var user = await _userManager.FindByIdAsync(input.UserId);
        if (user == null)
        {
            TempData["ManageUserError"] = "User not found.";
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort));
        }

        var normalizedEmail = input.Email.Trim();
        var existingEmailUser = await _userManager.FindByEmailAsync(normalizedEmail);
        if (existingEmailUser != null && existingEmailUser.Id != user.Id)
        {
            TempData["ManageUserError"] = "Another user is already using this email.";
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort));
        }

        user.FullName = input.FullName.Trim();
        user.Email = normalizedEmail;
        user.PhoneNumber = input.PhoneNumber.Trim();

        var userUpdateResult = await _userManager.UpdateAsync(user);
        if (!userUpdateResult.Succeeded)
        {
            TempData["ManageUserError"] = BuildIdentityErrorMessage(userUpdateResult);
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort));
        }

        var syncRoleResult = await SyncUserRoleAsync(user, role);
        if (!syncRoleResult.Succeeded)
        {
            TempData["ManageUserError"] = BuildIdentityErrorMessage(syncRoleResult);
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort));
        }

        await UpsertUserClaimsAsync(user, input.UserCode, input.Gender);

        TempData["ManageUserSuccess"] = "User updated successfully.";
        return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort));
    }

    [HttpPost("delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(ManageUserDeleteInput input)
    {
        if (!ModelState.IsValid)
        {
            TempData["ManageUserError"] = "Invalid delete request.";
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort));
        }

        var currentUserId = _userManager.GetUserId(User);
        if (string.Equals(currentUserId, input.UserId, StringComparison.Ordinal))
        {
            TempData["ManageUserError"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort));
        }

        var user = await _userManager.FindByIdAsync(input.UserId);
        if (user == null)
        {
            TempData["ManageUserError"] = "User not found.";
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort));
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            TempData["ManageUserError"] = BuildIdentityErrorMessage(result);
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort));
        }

        TempData["ManageUserSuccess"] = "User deleted successfully.";
        return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort));
    }

    private static object BuildIndexRouteValues(string? tab, string? search, string? gender, string? roleFilter, string? sort)
    {
        return new
        {
            tab = NormalizeTab(tab),
            search = (search ?? string.Empty).Trim(),
            gender = NormalizeGenderFilter(gender),
            roleFilter = NormalizeRoleFilter(roleFilter),
            sort = NormalizeSort(sort)
        };
    }

    private static string NormalizeTab(string? tab)
    {
        return string.Equals(tab, "staffs", StringComparison.OrdinalIgnoreCase)
               || string.Equals(tab, "staff", StringComparison.OrdinalIgnoreCase)
            ? "staffs"
            : "students";
    }

    private static string NormalizeGenderFilter(string? gender)
    {
        if (string.Equals(gender, "male", StringComparison.OrdinalIgnoreCase)
            || string.Equals(gender, "m", StringComparison.OrdinalIgnoreCase))
        {
            return "male";
        }

        if (string.Equals(gender, "female", StringComparison.OrdinalIgnoreCase)
            || string.Equals(gender, "f", StringComparison.OrdinalIgnoreCase))
        {
            return "female";
        }

        if (string.Equals(gender, "unspecified", StringComparison.OrdinalIgnoreCase)
            || string.Equals(gender, "u", StringComparison.OrdinalIgnoreCase))
        {
            return "unspecified";
        }

        return string.Empty;
    }

    private static string GetDisplayGenderForFilter(string gender)
    {
        return gender switch
        {
            "male" => "Male",
            "female" => "Female",
            "unspecified" => "Unspecified",
            _ => string.Empty
        };
    }

    private static string NormalizeRoleFilter(string? roleFilter)
    {
        if (string.Equals(roleFilter, "student", StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleFilter, StudentRole, StringComparison.OrdinalIgnoreCase))
        {
            return "student";
        }

        if (string.Equals(roleFilter, AdminRole, StringComparison.OrdinalIgnoreCase))
        {
            return "admin";
        }

        if (string.Equals(roleFilter, LibrarianRole, StringComparison.OrdinalIgnoreCase))
        {
            return "librarian";
        }

        return string.Empty;
    }

    private static bool IsRoleMatched(string role, string roleFilter)
    {
        return roleFilter switch
        {
            "student" => string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase),
            "admin" => string.Equals(role, AdminRole, StringComparison.OrdinalIgnoreCase),
            "librarian" => string.Equals(role, LibrarianRole, StringComparison.OrdinalIgnoreCase),
            _ => true
        };
    }

    private static string NormalizeSort(string? sort)
    {
        if (string.Equals(sort, "name_desc", StringComparison.OrdinalIgnoreCase))
        {
            return "name_desc";
        }

        if (string.Equals(sort, "id_asc", StringComparison.OrdinalIgnoreCase))
        {
            return "id_asc";
        }

        if (string.Equals(sort, "id_desc", StringComparison.OrdinalIgnoreCase))
        {
            return "id_desc";
        }

        return "name_asc";
    }

    private static IEnumerable<ManageUserItemViewModel> ApplySort(IEnumerable<ManageUserItemViewModel> items, string sort)
    {
        return sort switch
        {
            "name_desc" => items.OrderByDescending(x => x.FullName).ThenByDescending(x => x.UserCode),
            "id_asc" => items.OrderBy(x => x.UserCode),
            "id_desc" => items.OrderByDescending(x => x.UserCode),
            _ => items.OrderBy(x => x.FullName).ThenBy(x => x.UserCode)
        };
    }

    private static bool ContainsIgnoreCase(string source, string value)
    {
        return source.Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveRole(IReadOnlyCollection<string> roles)
    {
        if (roles.Any(r => string.Equals(r, AdminRole, StringComparison.OrdinalIgnoreCase)))
        {
            return AdminRole;
        }

        if (roles.Any(r => string.Equals(r, LibrarianRole, StringComparison.OrdinalIgnoreCase)))
        {
            return LibrarianRole;
        }

        if (roles.Any(r => string.Equals(r, StudentRole, StringComparison.OrdinalIgnoreCase)))
        {
            return StudentRole;
        }

        return StudentRole;
    }

    private static bool IsStaffRole(string role)
    {
        return string.Equals(role, AdminRole, StringComparison.OrdinalIgnoreCase)
               || string.Equals(role, LibrarianRole, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeGender(string? gender)
    {
        if (string.Equals(gender, "M", StringComparison.OrdinalIgnoreCase)
            || string.Equals(gender, "Male", StringComparison.OrdinalIgnoreCase))
        {
            return "Male";
        }

        if (string.Equals(gender, "F", StringComparison.OrdinalIgnoreCase)
            || string.Equals(gender, "Female", StringComparison.OrdinalIgnoreCase))
        {
            return "Female";
        }

        return "Unspecified";
    }

    private static string NormalizeGenderForStore(string gender)
    {
        if (string.Equals(gender, "M", StringComparison.OrdinalIgnoreCase)
            || string.Equals(gender, "Male", StringComparison.OrdinalIgnoreCase))
        {
            return "M";
        }

        if (string.Equals(gender, "F", StringComparison.OrdinalIgnoreCase)
            || string.Equals(gender, "Female", StringComparison.OrdinalIgnoreCase))
        {
            return "F";
        }

        return "U";
    }

    private static string NormalizeRole(string? role)
    {
        if (string.Equals(role, AdminRole, StringComparison.OrdinalIgnoreCase))
        {
            return AdminRole;
        }

        if (string.Equals(role, LibrarianRole, StringComparison.OrdinalIgnoreCase))
        {
            return LibrarianRole;
        }

        if (string.Equals(role, StudentRole, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
        {
            return StudentRole;
        }

        return string.Empty;
    }

    private static bool IsSupportedRole(string role)
    {
        return string.Equals(role, StudentRole, StringComparison.OrdinalIgnoreCase)
               || string.Equals(role, AdminRole, StringComparison.OrdinalIgnoreCase)
               || string.Equals(role, LibrarianRole, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeUserCode(string? rawCode, string userId)
    {
        var digits = new string((rawCode ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length == 7)
        {
            return digits;
        }

        if (digits.Length > 7)
        {
            return digits[^7..];
        }

        if (digits.Length > 0)
        {
            return digits.PadLeft(7, '0');
        }

        return GenerateFallbackCode(userId);
    }

    private static string GenerateFallbackCode(string userId)
    {
        var hash = 0;
        foreach (var ch in userId)
        {
            hash = ((hash * 31) + ch) % 10000000;
        }

        if (hash < 0)
        {
            hash *= -1;
        }

        return hash.ToString("D7");
    }

    private async Task<string> GenerateUniqueUsernameAsync(string fullName, string email)
    {
        var baseName = Regex.Replace(fullName ?? string.Empty, @"[^a-zA-Z0-9]", string.Empty);
        if (string.IsNullOrWhiteSpace(baseName))
        {
            var emailPrefix = (email ?? string.Empty).Split('@')[0];
            baseName = Regex.Replace(emailPrefix, @"[^a-zA-Z0-9]", string.Empty);
        }

        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "User";
        }

        var candidate = baseName;
        var suffix = 1;
        while (await _userManager.FindByNameAsync(candidate) != null)
        {
            suffix++;
            candidate = $"{baseName}{suffix}";
        }

        return candidate;
    }

    private async Task<IdentityResult> SyncUserRoleAsync(ApplicationUser user, string desiredRole)
    {
        var currentRoles = await _userManager.GetRolesAsync(user);

        var toRemove = currentRoles
            .Where(r => !string.Equals(r, desiredRole, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (toRemove.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, toRemove);
            if (!removeResult.Succeeded)
            {
                return removeResult;
            }
        }

        if (!currentRoles.Any(r => string.Equals(r, desiredRole, StringComparison.OrdinalIgnoreCase)))
        {
            var addResult = await _userManager.AddToRoleAsync(user, desiredRole);
            if (!addResult.Succeeded)
            {
                return addResult;
            }
        }

        return IdentityResult.Success;
    }

    private async Task UpsertUserClaimsAsync(ApplicationUser user, string userCode, string gender)
    {
        await UpsertClaimAsync(user, UserCodeClaimType, NormalizeUserCode(userCode, user.Id));
        await UpsertClaimAsync(user, GenderClaimType, NormalizeGenderForStore(gender));
    }

    private async Task UpsertClaimAsync(ApplicationUser user, string claimType, string claimValue)
    {
        var existingClaims = await _userManager.GetClaimsAsync(user);
        var existingClaim = existingClaims.FirstOrDefault(c => string.Equals(c.Type, claimType, StringComparison.Ordinal));
        var nextClaim = new Claim(claimType, claimValue);

        if (existingClaim == null)
        {
            await _userManager.AddClaimAsync(user, nextClaim);
            return;
        }

        if (string.Equals(existingClaim.Value, claimValue, StringComparison.Ordinal))
        {
            return;
        }

        await _userManager.ReplaceClaimAsync(user, existingClaim, nextClaim);
    }

    private static string BuildIdentityErrorMessage(IdentityResult result)
    {
        var message = string.Join(" ", result.Errors.Select(x => x.Description).Take(3));
        return string.IsNullOrWhiteSpace(message) ? "Operation failed." : message;
    }

    private string BuildModelStateErrorMessage()
    {
        var firstError = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(x => x.ErrorMessage)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        return string.IsNullOrWhiteSpace(firstError) ? "Please fill all required fields correctly." : firstError;
    }
}
