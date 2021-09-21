using LiteDB;
using LiteDB.Engine;
using RssCrawler.Models;
using RssCrawler.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RssCrawler
{
    public class SimpleFeedlyDatabaseAccess
    {
        public const string DbName = "RssCrawler.db";

        public static string GetDbPath()
        {
            var rootPath = EnvironmentHelper.GetApplicationRoot();

            return Path.Combine(rootPath, "../", DbName);
        }

        public static void Shrink()
        {
            using (var db = new LiteDatabase(GetDbPath()))
            {
                db.Rebuild();
            }
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

                //item.RssChannelDomainGroup = string.IsNullOrEmpty(item.RssChannelDomainGroup) ? item.Link : item.RssChannelDomainGroup;
                item.PublishingDate = item.PublishingDate == null || item.PublishingDate == DateTime.MinValue ? DateTime.Now : item.PublishingDate;

                col.Insert(item);
            }
        }

        public static void InsertFeedItems(List<RssFeedItemRow> items)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            using (var db = new LiteDatabase(GetDbPath()))
            {
                var col = db.GetCollection<RssFeedItemRow>("feedItems");

                foreach (var item in items)
                {
                    //item.RssChannelDomainGroup = string.IsNullOrEmpty(item.RssChannelDomainGroup) ? item.Link : item.RssChannelDomainGroup;
                    item.PublishingDate = item.PublishingDate == null || item.PublishingDate == DateTime.MinValue ? DateTime.Now : item.PublishingDate;
                }               

                col.InsertBulk(items);
            }
        }

        public static List<RssFeedItemRow> GetAllFeedItems()
        {
            using (var db = new LiteDatabase(GetDbPath()))
            {
                var col = db.GetCollection<RssFeedItemRow>("feedItems");

                return col
                    .Include(x => x.Channel)
                    .FindAll()
                    .Where(x=>x?.Channel?.Id != null)
                    .OrderBy(x => x.Channel.Id)
                    .ThenByDescending(x => x.PublishingDate)
                    ?.ToList() ?? new List<RssFeedItemRow>();
            }
        }

        public static bool IsBlackListWord(string md5String)
        {
            using (var db = new LiteDatabase(GetDbPath()))
            {
                var col = db.GetCollection<BlacklistRow>("blacklists");

                return col.Exists(x => x.ShrinkedTitleHash == md5String);
            }
        }

        public static bool IsExistedFeedItem(ObjectId channelId, string channelDomainGroup, string feedItemKey)
        {
            using (var db = new LiteDatabase(GetDbPath()))
            {
                var col = db.GetCollection<RssFeedItemRow>("feedItems");

                return col
                    .Include(x => x.Channel)
                    .Exists(x => x.Channel.Id == channelId && (x.Channel.DomainGroup == null ? x.Channel.Link : x.Channel.DomainGroup) == channelDomainGroup && x.FeedItemKey == feedItemKey);
            }
        }
    }
}
