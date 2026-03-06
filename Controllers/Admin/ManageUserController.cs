using System.Security.Claims;
using System.Text.RegularExpressions;
using Library_Management_system.Data;
using Library_Management_system.Models;
using Library_Management_system.Models.Admin;
using Library_Management_system.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services; // Add this namespace
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
    private const string AccountApprovalClaimType = AccountApproval.ClaimType;

    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly ITelegramNotifier _telegramNotifier;

    public ManageUserController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        ITelegramNotifier telegramNotifier)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _emailSender = emailSender;
        _telegramNotifier = telegramNotifier;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? tab,
        string? search,
        string? gender,
        string? roleFilter,
        string? sort,
        int pageStudents = 1,
        int pageStaffs = 1)
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
                        && (c.ClaimType == GenderClaimType ||
                            c.ClaimType == UserCodeClaimType ||
                            c.ClaimType == AccountApprovalClaimType))
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

        var approvalByUser = claims
            .Where(c => c.ClaimType == AccountApprovalClaimType)
            .GroupBy(c => c.UserId)
            .ToDictionary(g => g.Key, g => AccountApproval.Normalize(g.Last().ClaimValue));

        var items = users.Select(user =>
        {
            var roles = rolesByUser.TryGetValue(user.Id, out var mappedRoles) ? mappedRoles : new List<string>();
            var role = ResolveRole(roles);
            var isStaff = IsStaffRole(role);
            var rawGender = genderByUser.TryGetValue(user.Id, out var gender) ? gender : string.Empty;
            var rawCode = codeByUser.TryGetValue(user.Id, out var userCode) ? userCode : string.Empty;
            var rawApprovalStatus = approvalByUser.TryGetValue(user.Id, out var approvalStatus)
                ? approvalStatus
                : AccountApproval.Approved;

            return new ManageUserItemViewModel
            {
                UserId = user.Id,
                UserCode = NormalizeUserCode(rawCode, user.Id),
                FullName = string.IsNullOrWhiteSpace(user.FullName) ? user.UserName ?? "Unknown" : user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Gender = NormalizeGender(rawGender),
                Role = role == StudentRole ? "Student" : role,
                ApprovalStatus = isStaff ? AccountApproval.Approved : AccountApproval.Normalize(rawApprovalStatus),
                CreatedBy = string.IsNullOrWhiteSpace(user.CreatedBy) ? "-" : user.CreatedBy,
                CreatedDate = user.CreatedDate,
                IsStaff = isStaff
            };
        });

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            items = items.Where(item =>
                ContainsIgnoreCase(item.UserCode, searchText) ||
                ContainsIgnoreCase(item.FullName, searchText));
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

        var sortedStudents = ApplySort(
                itemList.Where(x => !x.IsStaff),
                normalizedSort
            )
            .ToList();

        var sortedStaffs = ApplySort(
                itemList.Where(x => x.IsStaff),
                normalizedSort
            )
            .ToList();

        var studentsPage = NormalizePage(pageStudents);
        var staffsPage = NormalizePage(pageStaffs);
        var pageSize = ManageUserPageViewModel.DefaultPageSize;

        var studentsTotalPages = Math.Max(1, (int)Math.Ceiling(sortedStudents.Count / (double)pageSize));
        var staffsTotalPages = Math.Max(1, (int)Math.Ceiling(sortedStaffs.Count / (double)pageSize));

        studentsPage = Math.Min(studentsPage, studentsTotalPages);
        staffsPage = Math.Min(staffsPage, staffsTotalPages);

        var students = sortedStudents
            .Skip((studentsPage - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var staffs = sortedStaffs
            .Skip((staffsPage - 1) * pageSize)
            .Take(pageSize)
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
            PageSize = pageSize,
            StudentsPage = studentsPage,
            StudentsTotalPages = studentsTotalPages,
            StudentsTotalCount = sortedStudents.Count,
            StaffsPage = staffsPage,
            StaffsTotalPages = staffsTotalPages,
            StaffsTotalCount = sortedStaffs.Count,
            Students = students,
            Staffs = staffs
        };

        return View("~/Views/Admin/ManageUser/Index.cshtml", model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ManageUserFormInput input)
    {
        input.UserCode = NormalizeSubmittedUserCode(input.UserCode);
        ModelState.Remove(nameof(ManageUserFormInput.UserCode));
        if (string.IsNullOrWhiteSpace(input.UserCode))
        {
            ModelState.AddModelError(nameof(input.UserCode), "ID must contain at least 1 digit.");
        }

        var role = NormalizeRole(input.Role);
        if (!IsSupportedRole(role))
        {
            TempData["ManageUserError"] = "Invalid role selected.";
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        if (!ModelState.IsValid)
        {
            TempData["ManageUserError"] = BuildModelStateErrorMessage();
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        if (await _userManager.FindByEmailAsync(input.Email.Trim()) != null)
        {
            TempData["ManageUserError"] = "A user with this email already exists.";
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        var user = new ApplicationUser
        {
            FullName = input.FullName.Trim(),
            Email = input.Email.Trim(),
            EmailConfirmed = true,
            PhoneNumber = input.PhoneNumber.Trim(),
            UserName = await GenerateUniqueUsernameAsync(input.FullName, input.Email),
            CreatedBy = GetCurrentActor(),
            CreatedDate = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, input.Password.Trim());
        if (!createResult.Succeeded)
        {
            TempData["ManageUserError"] = BuildIdentityErrorMessage(createResult);
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            TempData["ManageUserError"] = BuildIdentityErrorMessage(roleResult);
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        var claimsResult = await UpsertUserClaimsAsync(user, input.UserCode, input.Gender);
        if (!claimsResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            TempData["ManageUserError"] = BuildIdentityErrorMessage(claimsResult);
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        var approvalResult = await SetApprovalStatusAsync(user, AccountApproval.Approved);
        if (!approvalResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            TempData["ManageUserError"] = BuildIdentityErrorMessage(approvalResult);
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        var successMessage = "User created successfully.";

        // Don't fail user creation when SMTP is misconfigured/unavailable.
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action(
                    "ConfirmEmail",
                    "Account",
                    new { userId = user.Id, token },
                    Request.Scheme);

                if (!string.IsNullOrWhiteSpace(confirmationLink))
                {
                    await _emailSender.SendEmailAsync(
                        user.Email,
                        "Confirm your email",
                        $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");

                    successMessage = "User created successfully. A verification email has been sent.";
                }
                else
                {
                    successMessage = "User created successfully, but verification email could not be generated.";
                }
            }
            catch
            {
                successMessage = "User created successfully, but verification email could not be sent. Please check email settings.";
            }
        }

        TempData["ManageUserSuccess"] = successMessage;
        return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
    }

    [HttpPost("update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ManageUserUpdateInput input)
    {
        input.UserCode = NormalizeSubmittedUserCode(input.UserCode);
        ModelState.Remove(nameof(ManageUserUpdateInput.UserCode));

        var role = NormalizeRole(input.Role);
        if (!IsSupportedRole(role))
        {
            TempData["ManageUserError"] = "Invalid role selected.";
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        var user = await _userManager.FindByIdAsync(input.UserId);
        if (user == null)
        {
            TempData["ManageUserError"] = "User not found.";
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        if (string.IsNullOrWhiteSpace(input.UserCode))
        {
            var existingRawCode = await _dbContext.UserClaims
                .AsNoTracking()
                .Where(c => c.UserId == user.Id && c.ClaimType == UserCodeClaimType)
                .OrderByDescending(c => c.Id)
                .Select(c => c.ClaimValue)
                .FirstOrDefaultAsync();

            input.UserCode = NormalizeUserCode(existingRawCode, user.Id);
        }

        if (!ModelState.IsValid)
        {
            TempData["ManageUserError"] = BuildModelStateErrorMessage();
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        var normalizedEmail = input.Email.Trim();
        var existingEmailUser = await _userManager.FindByEmailAsync(normalizedEmail);
        if (existingEmailUser != null && existingEmailUser.Id != user.Id)
        {
            TempData["ManageUserError"] = "Another user is already using this email.";
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        user.FullName = input.FullName.Trim();
        user.Email = normalizedEmail;
        user.PhoneNumber = input.PhoneNumber.Trim();

        var userUpdateResult = await _userManager.UpdateAsync(user);
        if (!userUpdateResult.Succeeded)
        {
            TempData["ManageUserError"] = BuildIdentityErrorMessage(userUpdateResult);
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        var syncRoleResult = await SyncUserRoleAsync(user, role);
        if (!syncRoleResult.Succeeded)
        {
            TempData["ManageUserError"] = BuildIdentityErrorMessage(syncRoleResult);
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        var claimsResult = await UpsertUserClaimsAsync(user, input.UserCode, input.Gender);
        if (!claimsResult.Succeeded)
        {
            TempData["ManageUserError"] = BuildIdentityErrorMessage(claimsResult);
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        TempData["ManageUserSuccess"] = "User updated successfully.";
        return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
    }

    [HttpPost("approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(ManageUserDeleteInput input)
    {
        if (!ModelState.IsValid)
        {
            TempData["ManageUserError"] = "Invalid approve request.";
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        var user = await _userManager.FindByIdAsync(input.UserId);
        if (user == null)
        {
            TempData["ManageUserError"] = "User not found.";
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        if (!await IsStudentAsync(user))
        {
            TempData["ManageUserError"] = "Only student accounts can be approved or canceled.";
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        var syncResult = await SetApprovalStatusAsync(user, AccountApproval.Approved);
        if (!syncResult.Succeeded)
        {
            TempData["ManageUserError"] = BuildIdentityErrorMessage(syncResult);
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        await _telegramNotifier.SendAdminAlertAsync(
            string.Join('\n',
                "User account approved.",
                $"Name: {user.FullName}",
                $"Email: {user.Email}",
                $"Approved By: {GetCurrentActor()}",
                $"Approved (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"));

        TempData["ManageUserSuccess"] = "User approved successfully.";
        return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
    }

    [HttpPost("cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(ManageUserDeleteInput input)
    {
        if (!ModelState.IsValid)
        {
            TempData["ManageUserError"] = "Invalid cancel request.";
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        var currentUserId = _userManager.GetUserId(User);
        if (string.Equals(currentUserId, input.UserId, StringComparison.Ordinal))
        {
            TempData["ManageUserError"] = "You cannot cancel your own account.";
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        var user = await _userManager.FindByIdAsync(input.UserId);
        if (user == null)
        {
            TempData["ManageUserError"] = "User not found.";
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        if (!await IsStudentAsync(user))
        {
            TempData["ManageUserError"] = "Only student accounts can be approved or canceled.";
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        var syncResult = await SetApprovalStatusAsync(user, AccountApproval.Canceled);
        if (!syncResult.Succeeded)
        {
            TempData["ManageUserError"] = BuildIdentityErrorMessage(syncResult);
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        await _telegramNotifier.SendAdminAlertAsync(
            string.Join('\n',
                "User account canceled.",
                $"Name: {user.FullName}",
                $"Email: {user.Email}",
                $"Canceled By: {GetCurrentActor()}",
                $"Canceled (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"));

        TempData["ManageUserSuccess"] = "User canceled successfully.";
        return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
    }

    [HttpPost("delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(ManageUserDeleteInput input)
    {
        if (!ModelState.IsValid)
        {
            TempData["ManageUserError"] = "Invalid delete request.";
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        var currentUserId = _userManager.GetUserId(User);
        if (string.Equals(currentUserId, input.UserId, StringComparison.Ordinal))
        {
            TempData["ManageUserError"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        var user = await _userManager.FindByIdAsync(input.UserId);
        if (user == null)
        {
            TempData["ManageUserError"] = "User not found.";
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            TempData["ManageUserError"] = BuildIdentityErrorMessage(result);
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
        }

        TempData["ManageUserSuccess"] = "User deleted successfully.";
        return RedirectToAction(nameof(Index), BuildIndexRouteValues(input.ReturnTab, input.Search, input.FilterGender, input.RoleFilter, input.Sort, input.PageStudents, input.PageStaffs));
    }

    private static object BuildIndexRouteValues(
        string? tab,
        string? search,
        string? gender,
        string? roleFilter,
        string? sort,
        int pageStudents = 1,
        int pageStaffs = 1)
    {
        return new
        {
            tab = NormalizeTab(tab),
            search = (search ?? string.Empty).Trim(),
            gender = NormalizeGenderFilter(gender),
            roleFilter = NormalizeRoleFilter(roleFilter),
            sort = NormalizeSort(sort),
            pageStudents = NormalizePage(pageStudents),
            pageStaffs = NormalizePage(pageStaffs)
        };
    }

    private static int NormalizePage(int page)
    {
        return page <= 0 ? 1 : page;
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

    private static string ResolveRole(IEnumerable<string> roles)
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

    private static string NormalizeSubmittedUserCode(string? rawCode)
    {
        var digits = new string((rawCode ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
        {
            return string.Empty;
        }

        if (digits.Length > 7)
        {
            return digits[^7..];
        }

        return digits.PadLeft(7, '0');
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

    private async Task<bool> IsStudentAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var resolvedRole = ResolveRole(roles);
        return !IsStaffRole(resolvedRole);
    }

    private async Task<IdentityResult> SetApprovalStatusAsync(ApplicationUser user, string status)
    {
        var normalizedStatus = AccountApproval.Normalize(status);

        if (normalizedStatus == AccountApproval.Canceled || normalizedStatus == AccountApproval.Pending)
        {
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
        }
        else
        {
            user.LockoutEnabled = true;
            user.LockoutEnd = null;
        }

        var userUpdateResult = await _userManager.UpdateAsync(user);
        if (!userUpdateResult.Succeeded)
        {
            return userUpdateResult;
        }

        return await UpsertClaimAsync(user, AccountApprovalClaimType, normalizedStatus);
    }

    private async Task<IdentityResult> UpsertUserClaimsAsync(ApplicationUser user, string userCode, string gender)
    {
        var codeResult = await UpsertClaimAsync(user, UserCodeClaimType, NormalizeUserCode(userCode, user.Id));
        if (!codeResult.Succeeded)
        {
            return codeResult;
        }

        return await UpsertClaimAsync(user, GenderClaimType, NormalizeGenderForStore(gender));
    }

    private async Task<IdentityResult> UpsertClaimAsync(ApplicationUser user, string claimType, string claimValue)
    {
        var existingClaims = await _userManager.GetClaimsAsync(user);
        var existingClaim = existingClaims.FirstOrDefault(c => string.Equals(c.Type, claimType, StringComparison.Ordinal));
        var nextClaim = new Claim(claimType, claimValue);

        if (existingClaim == null)
        {
            return await _userManager.AddClaimAsync(user, nextClaim);
        }

        if (string.Equals(existingClaim.Value, claimValue, StringComparison.Ordinal))
        {
            return IdentityResult.Success;
        }

        return await _userManager.ReplaceClaimAsync(user, existingClaim, nextClaim);
    }

    private static string BuildIdentityErrorMessage(IdentityResult result)
    {
        var message = string.Join(" ", result.Errors.Select(x => x.Description).Take(3));
        return string.IsNullOrWhiteSpace(message) ? "Operation failed." : message;
    }

    private string GetCurrentActor()
    {
        var actor = User?.Identity?.Name?.Trim();
        return string.IsNullOrWhiteSpace(actor) ? "System Admin" : actor;
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

