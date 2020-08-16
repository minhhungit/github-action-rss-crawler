using LiteDB;
using System;

namespace RssCrawler.Models
{
    public class RssFeedItemRow
    {
        public ObjectId Id { get; set; }

        [BsonRef("channels")]
        public RssChannelRow Channel { get; set; }
        public string FeedItemKey { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public string Description { get; set; }
        public DateTime PublishingDate { get; set; }
        public string Author { get; set; }
        public string Content { get; set; }
        public bool IsChecked { get; set; }

        public string CoverImageUrl { get; set; }
        public string XmlData { get; set; }
    }
}
