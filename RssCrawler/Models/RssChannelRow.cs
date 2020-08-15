using LiteDB;
using System;

namespace RssCrawler.Models
{
   public class RssChannelRow
    {
        public ObjectId Id { get; set; }
        public int Type { get; set; }
        public string DomainGroup { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public string Description { get; set; }
        public string Language { get; set; }
        public string Copyright { get; set; }
        public DateTime LastUpdatedDate { get; set; }
        public string ImageUrl { get; set; }
        public string OriginalDocument { get; set; }
        public bool IsError { get; set; }
        public string ErrorMessage { get; set; }
        public int IsActive { get; set; }
        public RssCrawlerEngine RssCrawlerEngine { get; set; }
        public int? RefreshTimeMinutes { get; set; }
    }
}
