using Library_Management_system.Models;

namespace Library_Management_system.Models;

public class HomeViewModel
{
    public List<Category> Categories { get; set; } = new();
    public List<Book> TrendingBooks { get; set; } = new();
    public List<Book> NewArrivalBooks { get; set; } = new();
    public List<Category> CategoryGenres { get; set; } = new();
}
