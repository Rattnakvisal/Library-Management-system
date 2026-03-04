using Library_Management_system.Models;

namespace Library_Management_system.Models;

public class HomeViewModel
{
    public List<string> Categories { get; set; } = new();
    public List<Book> TrendingBooks { get; set; } = new();
    public List<Book> NewArrivalBooks { get; set; } = new();
    public List<string> Genres { get; set; } = new();
}