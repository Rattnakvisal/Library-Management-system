namespace Library_Management_system.Models.Admin;

public sealed class ManageFeedbackPageViewModel
{
    public string Search { get; set; } = string.Empty;
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalMessages { get; set; }
    public int UnreadMessages { get; set; }
    public IReadOnlyList<ManageFeedbackMessageItemViewModel> Messages { get; set; } =
        Array.Empty<ManageFeedbackMessageItemViewModel>();

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}

public sealed class ManageFeedbackMessageItemViewModel
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedDate { get; set; }
}
