using LiteDB;
using RssCrawler.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RssCrawler
{
    public class SimpleFeedlyDatabaseAccess
    {
        public const string DbName = "RssCrawler.db";

        public static string GetDbPath()
        {
            var rootPath = GetApplicationRoot();

            return Path.Combine(rootPath, "../", DbName);
        }

        public static string GetApplicationRoot()
        {
            var exePath = Path.GetDirectoryName(System.Reflection
                              .Assembly.GetExecutingAssembly().CodeBase);
            Regex appPathMatcher = new Regex(@"(?<!fil)[A-Za-z]:\\+[\S\s]*?(?=\\+bin)");
            var appRoot = appPathMatcher.Match(exePath).Value;
            return appRoot;
        }

        public static List<RssChannelRow> GetActiveChannels()
        {
            var result = new List<RssChannelRow>();
            using (var db = new LiteDatabase(GetDbPath()))
            {
                var col = db.GetCollection<RssChannelRow>("channels");

                // Create unique index in Link field
                col.EnsureIndex(x => x.Link, true);

                return col.Find(x => x.IsActive == 1)?.ToList() ?? new List<RssChannelRow>();
            }
        }

        public static void UpdateChannelDefaultEngine(ObjectId channelId, RssCrawlerEngine engine)
        {
            using (var db = new LiteDatabase(GetDbPath()))
            {
                var col = db.GetCollection<RssChannelRow>("channels");

                // Create unique index in Id field
                col.EnsureIndex(x => x.Id, true);

                //  UPDATE dbo.RssChannels SET RssCrawlerEngine = @engine WHERE Id = @channelId

                var channel = col.FindById(channelId);
                channel.RssCrawlerEngine = engine;
                col.Update(channel);
            }
        }

        public static void UpdateChannelErrorStatus(ObjectId channelId, bool isError, string errorMessage)
        {
            if (channelId == null)
            {
                return;
            }

            using (var db = new LiteDatabase(GetDbPath()))
            {
                var col = db.GetCollection<RssChannelRow>("channels");
                var channel = col.FindById(channelId);

                channel.IsError = isError;
                channel.ErrorMessage = errorMessage;

                col.Update(channel);
            }
        }

        public static void DeleteAllFeedItemByChannelId(ObjectId channelId)
        {
            using (var db = new LiteDatabase(GetDbPath()))
            {
                var col = db.GetCollection<RssFeedItemRow>("feedItems");
                col.DeleteMany(x => x.Channel.Id == channelId);
            }
        }

        public static void InsertFeedItem(RssFeedItemRow item)
        {
            using (var db = new LiteDatabase(GetDbPath()))
            {
                var col = db.GetCollection<RssFeedItemRow>("feedItems");

                item.RssChannelDomainGroup = string.IsNullOrEmpty(item.RssChannelDomainGroup) ? item.Link : item.RssChannelDomainGroup;
                item.PublishingDate = item.PublishingDate == null || item.PublishingDate == DateTime.MinValue ? DateTime.Now : item.PublishingDate;

                col.Insert(item);
            }
        }
    }
}
