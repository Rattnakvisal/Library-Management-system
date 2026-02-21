namespace Library_Management_system.Models.Admin
{
    public sealed class ManageCategoryViewModel
    {
        public string Name { get; set; } = string.Empty;
        public int BookCount { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? CreatedDate { get; set; }
    }
}
