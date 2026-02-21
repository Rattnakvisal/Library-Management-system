using System.ComponentModel.DataAnnotations;

namespace Library_Management_system.Models.Admin;

public class ManageUserPageViewModel
{
    public int TotalStudents { get; set; }
    public int TotalStaff { get; set; }
    public int TotalUsers { get; set; }
    public string ActiveTab { get; set; } = "students";
    public string Search { get; set; } = string.Empty;
    public IReadOnlyList<ManageUserItemViewModel> Students { get; set; } = Array.Empty<ManageUserItemViewModel>();
    public IReadOnlyList<ManageUserItemViewModel> Staffs { get; set; } = Array.Empty<ManageUserItemViewModel>();
}

public class ManageUserItemViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string UserCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsStaff { get; set; }
}

public class ManageUserFormInput
{
    [Required]
    [StringLength(7, MinimumLength = 7)]
    [RegularExpression(@"^\d{7}$", ErrorMessage = "ID must be exactly 7 digits.")]
    public string UserCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string Gender { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Role { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnTab { get; set; } = "students";
    public string? Search { get; set; }
}

public class ManageUserUpdateInput
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(7, MinimumLength = 7)]
    [RegularExpression(@"^\d{7}$", ErrorMessage = "ID must be exactly 7 digits.")]
    public string UserCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string Gender { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Role { get; set; } = string.Empty;

    public string? ReturnTab { get; set; } = "students";
    public string? Search { get; set; }
}

public class ManageUserDeleteInput
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    public string? ReturnTab { get; set; } = "students";
    public string? Search { get; set; }
}
