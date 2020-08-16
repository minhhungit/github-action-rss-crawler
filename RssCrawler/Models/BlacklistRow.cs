using LiteDB;

namespace RssCrawler.Models
{
    public class BlacklistRow
    {
        public ObjectId Id { get; set; }
        public string ShrinkedTitle { get; set; }
        public string ShrinkedTitleHash { get; set; }
    }
}
