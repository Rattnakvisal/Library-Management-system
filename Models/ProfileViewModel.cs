namespace Library_Management_system.Models;

public sealed class ProfileViewModel
{
    public string FullName { get; set; } = "User";
    public string Email { get; set; } = string.Empty;
    public string MembershipLabel { get; set; } = "Member";
    public string ProfileImageUrl { get; set; } =
        "https://i.pinimg.com/736x/c0/d1/9f/c0d19f67852cb44fe9bdbed793141790.jpg";
    public string CoverImageUrl { get; set; } =
        "https://i.pinimg.com/736x/61/0d/c5/610dc55e2fb7a1ab8728a718be63e1d4.jpg";
    public IReadOnlyList<ProfileInterestItemViewModel> Interests { get; set; } =
        Array.Empty<ProfileInterestItemViewModel>();
}

public sealed class ProfileInterestItemViewModel
{
    public int BookId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int Rating { get; set; }
}
