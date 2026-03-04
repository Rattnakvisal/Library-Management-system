using Library_Management_system.Models;

namespace Library_Management_system.Models;

public class HomeViewModel
{
    public List<HomeCategoryCardViewModel> Categories { get; set; } = new();
    public List<Book> TrendingBooks { get; set; } = new();
    public List<Book> NewArrivalBooks { get; set; } = new();
    public List<HomeGenreAuthorCardViewModel> Genres { get; set; } = new();
}

public class HomeCategoryCardViewModel
{
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public class HomeGenreAuthorCardViewModel
{
    public string GenreName { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}
