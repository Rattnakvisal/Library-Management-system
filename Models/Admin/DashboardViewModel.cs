namespace Library_Management_system.Models.Admin
{
    public sealed class DashboardViewModel
    {
        public int TotalBooks { get; set; }
        public int BorrowedBooks { get; set; }
        public int TotalUsers { get; set; }
        public decimal TotalFines { get; set; }
        public IReadOnlyList<DashboardCategoryChartItemViewModel> CategoryDistribution { get; set; } =
            Array.Empty<DashboardCategoryChartItemViewModel>();
        public IReadOnlyList<DashboardNewMemberItemViewModel> NewMembers { get; set; } =
            Array.Empty<DashboardNewMemberItemViewModel>();
        public IReadOnlyList<DashboardNewBookItemViewModel> NewBooks { get; set; } =
            Array.Empty<DashboardNewBookItemViewModel>();
    }

    public sealed class DashboardCategoryChartItemViewModel
    {
        public string CategoryName { get; set; } = string.Empty;
        public int BookCount { get; set; }
    }

    public sealed class DashboardNewMemberItemViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Gender { get; set; } = "Unspecified";
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime? CreatedDate { get; set; }
    }

    public sealed class DashboardNewBookItemViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime? CreatedDate { get; set; }
    }
}
