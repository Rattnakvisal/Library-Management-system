using System.Collections.Generic;

namespace Library_Management_system.Models
{
    public class BookDetailViewModel
    {
        public Book Book { get; set; } = new();
        public List<Book> RelatedBooks { get; set; } = new();
        public bool IsFavorite { get; set; }
        public HashSet<int> RelatedFavoriteBookIds { get; set; } = new();
    }
}
