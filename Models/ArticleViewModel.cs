namespace LearnConnect.Models
{
    public class ArticleViewModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string ImageUrl { get; set; }
        public DateTime PublishedAt { get; set; }
        public string Source { get; set; } 
        public string FormattedDate => PublishedAt.ToLocalTime().ToString("MMM d, yyyy");
        public string FormattedTime => PublishedAt.ToLocalTime().ToString("h:mm tt");
        public string ShortDescription => Description?.Length > 120
            ? Description.Substring(0, 117) + "..."
            : Description;
    }
}
