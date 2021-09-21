using HtmlAgilityPack;
using LiteDB;
using Newtonsoft.Json;
using RssCrawler.Models;
using RssCrawler.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace RssCrawler
{
    public class RssCrawler
    {
        const string LogDateTimeFormat = "yyyyMMdd_HHmmss";

        public static void CrawlRss()
        {
            var logFolder = $"{Path.Combine(EnvironmentHelper.GetApplicationRoot(), "../Logs/")}";
            if (!Directory.Exists(logFolder))
            {
                Directory.CreateDirectory(logFolder);
            }

            var existedLogFiles = Directory.GetFiles(logFolder);

            var logFileBank = new Dictionary<DateTime, string>();
            foreach (var file in existedLogFiles)
            {
                var dtFormatArr = Path.GetFileNameWithoutExtension(file).Split('-');
                if (dtFormatArr.Length > 0)
                {
                    var dtText = dtFormatArr[0];
                    if (DateTime.TryParseExact(dtText, LogDateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                    {
                        logFileBank.Add(dt, file);
                    }
                }                
            }

            foreach (var kv in logFileBank)
            {
                if (kv.Key < DateTime.Now.AddDays(-10)) // just keep 10 days nearest
                {
                    File.Delete(kv.Value);
                }
            }

            var logPath = $"{Path.Combine(logFolder, $"{DateTime.Now.ToString(LogDateTimeFormat)}-crawler.txt")}";

            var _logger = new MyLogger(logPath);

            var feedUrl = string.Empty;
            try
            {
                List<RssChannelRow> channels = channels = SimpleFeedlyDatabaseAccess.GetActiveChannels().OrderBy(x => x.Id).ToList();
                
                _logger.Info($"There are {channels.Count} active channels");

                // just take top 5 channel
                // purpose of this repository is demo how to run dotnet core app on github action, not rss crawl
                channels = channels.Take(5).ToList();

                var progressCounter = 0;
                foreach (var channel in channels)
                {
                    progressCounter++;

                    feedUrl = channel.Link;

                    if (string.IsNullOrWhiteSpace(feedUrl))
                    {
                        continue;
                    }

                    try
                    {
                        _logger.Info($"- [{progressCounter}/{channels.Count}] Fetching url {feedUrl}");
                        var feed = GetFeedsFromChannel(feedUrl, channel.RssCrawlerEngine, out RssCrawlerEngine usedEngine, out Exception fetchFeedError);

                        _logger.Info($"  - Nbr of feed items {feed?.Items?.Count ?? 0}");

                        // update default engine for channel
                        SimpleFeedlyDatabaseAccess.UpdateChannelDefaultEngine(channel.Id, fetchFeedError != null ? RssCrawlerEngine.CodeHollowFeedReader : usedEngine);

                        if (feed != null && feed?.Items != null)
                        {
                            var top5LatestItems = feed.Items
                                .OrderByDescending(x => x.PublishingDate)
                                .Take(5)
                                .ToList();

                            if (top5LatestItems.Count == 0)
                            {
                                continue;
                            }
                            else
                            {
                                SimpleFeedlyDatabaseAccess.DeleteAllFeedItemByChannelId(channel.Id);
                                _logger.Info($"  - Deleted old items");

                                var insertItems = new List<RssFeedItemRow>();

                                foreach (var fItem in top5LatestItems)
                                {
                                    if (!StringUtils.IsUrl(fItem.Link))
                                    {
                                        continue;
                                    }

                                    var feedItemKey = GenerateFeedItemKey(fItem);

                                    if (string.IsNullOrWhiteSpace(feedItemKey) || string.IsNullOrWhiteSpace(fItem.Link))
                                    {
                                        continue;
                                    }

                                    var feedItem = new RssFeedItemRow
                                    {
                                        Channel = channel,
                                        FeedItemKey = feedItemKey,
                                        Title = string.IsNullOrWhiteSpace(fItem.Title) ? fItem.Link : fItem.Title,
                                        Link = fItem.Link,
                                        Description = fItem.Description,
                                        PublishingDate = fItem.PublishingDate,
                                        Author = fItem.Author
                                    };
                                    
                                    var shrinkedTitle = StringUtils.UnsignString(StringUtils.RemoveNonAlphaCharactersAndDigit(feedItem.Title)).ToLower();
                                    var shrinkedTitleHash = StringUtils.MD5Hash(shrinkedTitle);

                                    if (!SimpleFeedlyDatabaseAccess.IsBlackListWord(shrinkedTitleHash))
                                    {
                                        var channelDomainGroup = string.IsNullOrEmpty(channel.DomainGroup) ? channel.Link : channel.DomainGroup;

                                        if (!SimpleFeedlyDatabaseAccess.IsExistedFeedItem(channel.Id, channelDomainGroup, feedItem.FeedItemKey))
                                        {
                                            //var coverImageUrl = fItem.GetFeedCoverImage();
                                            //if (!string.IsNullOrWhiteSpace(coverImageUrl))
                                            //{
                                            //    feedItem.CoverImageUrl = coverImageUrl;
                                            //}

                                            insertItems.Add(feedItem);
                                        }                                        
                                    }
                                }

                                SimpleFeedlyDatabaseAccess.InsertFeedItems(insertItems);

                                _logger.Info($"  - Inserted {insertItems.Count()} items");
                            }

                            SimpleFeedlyDatabaseAccess.UpdateChannelErrorStatus(channel.Id, false, null);
                            _logger.Info($"  - Updated status");
                        }
                        else
                        {
                            _logger.Info($"  - [NO ITEMS]");
                            SimpleFeedlyDatabaseAccess.UpdateChannelErrorStatus(channel.Id, true, fetchFeedError == null ? null : JsonConvert.SerializeObject(fetchFeedError));

                            if (fetchFeedError != null)
                            {
                                ErrorHandle(fetchFeedError, feedUrl);
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        _logger.Error($"  - Got Error: {JsonConvert.SerializeObject(err, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore })}");

                        SimpleFeedlyDatabaseAccess.UpdateChannelErrorStatus(channel.Id, true, JsonConvert.SerializeObject(err));
                        ErrorHandle(err, feedUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"  - [ERROR]: {JsonConvert.SerializeObject(ex, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore })}");

                ErrorHandle(ex, feedUrl);
            }

            _logger.Info($"Done!");
        }

        private static string GenerateFeedItemKey(SimpleFeedlyFeedItem item)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                if (string.IsNullOrWhiteSpace(item.Link))
                {
                    return null;
                }
                else
                {
                    return item.Link;
                }
            }
            else
            {
                return item.Id;
            }
        }

        private static void ErrorHandle(Exception ex, string feedUrl)
        {
            // we can send an email for warning right here if needed

            // or just log error into some error stores, it's up to you
            //ErrorStore.LogExceptionWithoutContext(ex, false, false,
            //                           new Dictionary<string, string>
            //                           {
            //                               {"feedUrl", feedUrl }
            //                           });
        }

        /// <summary>
        /// GetFeedsFromChannel
        /// </summary>
        /// <param name="feedUrl">feed Url</param>
        /// <param name="defaultEngineType">crawler engine</param>
        /// <param name="isRest">
        /// Normally we call this method two times, first times with 'default' channel's crawler engine, and the last times for the rest crawler engine
        /// isRest = false: FIRST TIMES
        /// isRest = true: LAST TIMES
        /// </param>
        /// <param name="engineTypeResult"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public static SimpleFeedlyFeed GetFeedsFromChannel(string feedUrl, RssCrawlerEngine defaultEngineType, out RssCrawlerEngine engineTypeResult, out Exception error)
        {
            IRssEngine getEngine(RssCrawlerEngine type)
            {
                IRssEngine tmpEngine = null;
                switch (type)
                {
                    case RssCrawlerEngine.SyndicationFeed:
                        tmpEngine = new SyndicationFeedEngine();
                        break;
                    case RssCrawlerEngine.CodeHollowFeedReader:
                        tmpEngine = new CodeHollowFeedReaderEngine();
                        break;
                    case RssCrawlerEngine.ParseRssByXml:
                        tmpEngine = new ParseRssByXmlEngine();
                        break;
                    default:

                        break;
                }

                if (tmpEngine == null)
                {
                    throw new Exception($"Can not find crawler engine for type <{type}>");
                }

                return tmpEngine;
            }

            RssCrawlerEngine currentEngineType = RssCrawlerEngine.CodeHollowFeedReader;
            var items = new List<SimpleFeedlyFeedItem>();

            try
            {
                // check default engine first
                IRssEngine rssEngine = getEngine(defaultEngineType);
                var feedItems = rssEngine.GetItems(feedUrl, out error);

                if (error == null && feedItems.Count > 0) // no error
                {
                    currentEngineType = defaultEngineType;
                    items = feedItems ?? new List<SimpleFeedlyFeedItem>();
                }
                else
                {
                    // check the rest engines
                    error = null;

                    foreach (RssCrawlerEngine engineLoop in (RssCrawlerEngine[])Enum.GetValues(typeof(RssCrawlerEngine)))
                    {
                        if (engineLoop == defaultEngineType)
                        {
                            continue;
                        }

                        currentEngineType = engineLoop;

                        rssEngine = getEngine(engineLoop);
                        feedItems = rssEngine.GetItems(feedUrl, out error);

                        items.AddRange(feedItems ?? new List<SimpleFeedlyFeedItem>());

                        if (error == null && feedItems.Count > 0) // no error
                        {
                            items = feedItems ?? new List<SimpleFeedlyFeedItem>();
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }

            engineTypeResult = currentEngineType;
            return new SimpleFeedlyFeed { Items = items ?? new List<SimpleFeedlyFeedItem>() };
        }
    }


    public class SimpleFeedlyFeed
    {
        public SimpleFeedlyFeed()
        {
            Items = new List<SimpleFeedlyFeedItem>();
        }

        public List<SimpleFeedlyFeedItem> Items { get; set; }
    }

    public class SimpleFeedlyFeedItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public string Description { get; set; }
        public DateTime PublishingDate { get; set; }
        public string Author { get; set; }
        public string Content { get; set; }

        //public string XmlData { get; set; }

        public string GetFeedCoverImage()
        {
            string imageUrl = string.Empty;
            HtmlDocument doc = new HtmlDocument();

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Link ?? string.Empty);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                string pageSource = string.Empty;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Stream receiveStream = response.GetResponseStream();
                    StreamReader readStream;
                    if (string.IsNullOrWhiteSpace(response.CharacterSet))
                        readStream = new StreamReader(receiveStream);
                    else
                        readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));

                    pageSource = readStream.ReadToEnd();

                    response.Close();
                    readStream.Close();
                }

                string metaImageRegexPattern = @"<meta[\s]+[^>]*?property[\s]?=[\s""']+og:image[\s""']+content[\s]?=[\s""']+(.*?)[""']+.*?>";
                var metaRegex = new Regex(metaImageRegexPattern, RegexOptions.IgnoreCase);
                var mDesc = metaRegex.Match(pageSource ?? string.Empty);
                if (mDesc.Success && mDesc.Groups.Count >= 2)
                {
                    imageUrl = mDesc.Groups[1]?.Value ?? string.Empty;
                }
            }
            catch { }

            try
            {
                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    string xpath = "//meta[@property='og:image']";
                    doc = new HtmlWeb().Load(Link ?? string.Empty);
                    var ogImage = doc.DocumentNode.SelectSingleNode(xpath);

                    imageUrl = ogImage?.Attributes["content"]?.Value?.ToString() ?? string.Empty;
                }
            }
            catch
            {

            }

            try
            {
                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    //string pattern = @"<img\s+[^>]*?src=('|')([^'']+)\1";
                    string pattern = "<img.+?src=[\"'](.+?)[\"'].*?>";
                    Regex myRegex = new Regex(pattern, RegexOptions.IgnoreCase);

                    // use Desc
                    if (!string.IsNullOrWhiteSpace(Description))
                    {
                        try
                        {
                            var mDesc = myRegex.Match(Description);

                            if (mDesc.Success && mDesc.Groups.Count >= 2)
                            {
                                imageUrl = mDesc.Groups[1]?.Value ?? string.Empty;
                            }
                        }
                        catch { }
                    }
                    else
                    {
                        // use Content
                        if (!string.IsNullOrWhiteSpace(Content))
                        {
                            try
                            {
                                Match mContent = myRegex.Match(Content);

                                if (mContent.Success && mContent.Groups.Count >= 2)
                                {
                                    imageUrl = mContent.Groups[1]?.Value ?? string.Empty;
                                }
                            }
                            catch { }
                        }
                        else
                        {
                            // last chance: full html
                            if (doc != null && !string.IsNullOrWhiteSpace(doc?.Text))
                            {
                                Match m = myRegex.Match(doc.Text);

                                if (m.Success && m.Groups.Count >= 2)
                                {
                                    imageUrl = m.Groups[1]?.Value ?? string.Empty;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {

            }

            //if (!string.IsNullOrWhiteSpace(XmlData))
            //{
            //    try
            //    {
            //        Match m = myRegex.Match(XmlData);

            //        if (m.Success && m.Groups.Count >= 2)
            //        {
            //            var tmp = m.Groups[1]?.Value ?? string.Empty;
            //            if (!string.IsNullOrWhiteSpace(tmp))
            //            {
            //                return tmp;
            //            }
            //        }
            //    }
            //    catch { }
            //}

            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                if (!imageUrl.StartsWith("http"))
                {
                    Uri pageUri = new Uri(Link);
                    return $"{pageUri.Scheme + Uri.SchemeDelimiter + pageUri.Host + ":" + pageUri.Port}/{imageUrl.TrimStart('/')}";
                }
                else
                {
                    return imageUrl;
                }
            }

            return string.Empty;
        }
    }

    public interface IRssEngine
    {
        List<SimpleFeedlyFeedItem> GetItems(string feedUrl, out Exception error);
    }

    internal class CodeHollowFeedReaderEngine : IRssEngine
    {
        public List<SimpleFeedlyFeedItem> GetItems(string feedUrl, out Exception error)
        {
            Exception currentEx = null;
            List<SimpleFeedlyFeedItem> items = new List<SimpleFeedlyFeedItem>();

            try
            {
                var feed = CodeHollow.FeedReader.FeedReader.ReadAsync(feedUrl).GetAwaiter().GetResult();

                foreach (var item in feed.Items)
                {
                    var feedItem = new SimpleFeedlyFeedItem
                    {
                        Id = item.Id,
                        Title = string.IsNullOrWhiteSpace(item.Title) ? item.Link : item.Title,
                        Link = item.Link,
                        Description = item.Description,
                        PublishingDate = item.PublishingDate ?? DateTime.Now,
                        Author = item.Author,
                        Content = item.Content
                    };

                    items.Add(feedItem);
                }
            }
            catch (Exception ex)
            {
                currentEx = ex;
            }

            error = currentEx;
            return items;
        }
    }

    internal class SyndicationFeedEngine : IRssEngine
    {
        public List<SimpleFeedlyFeedItem> GetItems(string feedUrl, out Exception error)
        {
            Exception currentEx = null;
            List<SimpleFeedlyFeedItem> items = new List<SimpleFeedlyFeedItem>();
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Parse;

            try
            {
                using (var reader = XmlReader.Create(feedUrl, settings))
                {
                    var feed = System.ServiceModel.Syndication.SyndicationFeed.Load(reader);
                    reader.Close();

                    foreach (System.ServiceModel.Syndication.SyndicationItem item in feed.Items)
                    {
                        var feedItem = new SimpleFeedlyFeedItem();

                        var link = item.Links.FirstOrDefault()?.Uri.ToString();
                        link = string.IsNullOrWhiteSpace(link) ? item.Id : link;

                        feedItem.Id = item.Id;
                        feedItem.Title = string.IsNullOrWhiteSpace(item.Title?.Text) ? link : item.Title.Text;
                        feedItem.Link = link;
                        feedItem.Description = item.Summary?.Text;
                        feedItem.PublishingDate = item.PublishDate.UtcDateTime;
                        feedItem.Author = item.Authors.FirstOrDefault()?.Name ?? string.Empty;
                        feedItem.Content = item.Content?.ToString();

                        //feedItem.XmlData = item.GetRss20Formatter().ToString();

                        items.Add(feedItem);
                    }
                }
            }
            catch (Exception ex)
            {
                currentEx = ex;
            }

            error = currentEx;
            return items;
        }
    }

    internal class ParseRssByXmlEngine : IRssEngine
    {
        public List<SimpleFeedlyFeedItem> GetItems(string feedUrl, out Exception error)
        {
            Exception currentEx = null;
            List<SimpleFeedlyFeedItem> items = new List<SimpleFeedlyFeedItem>();

            try
            {
                var xmlString = string.Empty;
                using (WebClient client = new WebClient())
                {
                    var htmlData = client.DownloadData(feedUrl);
                    xmlString = System.Text.Encoding.UTF8.GetString(htmlData);

                    // ReplaceHexadecimalSymbols
                    string r = "[\x00-\x08\x0B\x0C\x0E-\x1F\x26]";
                    xmlString = Regex.Replace(xmlString, r, "", RegexOptions.Compiled);
                }

                XmlDocument rssXmlDoc = new XmlDocument();
                rssXmlDoc.LoadXml(xmlString);

                // Parse the Items in the RSS file
                XmlNodeList rssNodes = rssXmlDoc.SelectNodes("rss/channel/item");

                var namespaceManager = new XmlNamespaceManager(rssXmlDoc.NameTable);
                var contentNamespace = rssXmlDoc.DocumentElement.GetAttribute("xmlns:content");
                namespaceManager.AddNamespace("content", contentNamespace);

                // Iterate through the items in the RSS file
                foreach (XmlNode rssNode in rssNodes)
                {
                    var feedItem = new SimpleFeedlyFeedItem();

                    XmlNode rssSubNode = rssNode.SelectSingleNode("link");
                    feedItem.Link = rssSubNode != null ? rssSubNode.InnerText : null;

                    rssSubNode = rssNode.SelectSingleNode("title");
                    feedItem.Title = rssSubNode != null ? rssSubNode.InnerText : null;
                    feedItem.Title = string.IsNullOrWhiteSpace(feedItem.Title) ? feedItem.Link : feedItem.Title;

                    rssSubNode = rssNode.SelectSingleNode("description");
                    feedItem.Description = rssSubNode != null ? rssSubNode.InnerText : null;

                    rssSubNode = rssNode.SelectSingleNode("//content:encoded", namespaceManager);
                    feedItem.Content = rssSubNode != null ? rssSubNode.InnerText : null;

                    rssSubNode = rssNode.SelectSingleNode("pubDate");
                    DateTime pubDate = DateTime.Now;

                    if (rssSubNode != null)
                    {
                        if (DateTime.TryParse(rssSubNode.InnerText, out DateTime tmpDate))
                        {
                            pubDate = tmpDate;
                        }
                    }

                    feedItem.PublishingDate = pubDate;


                    //feedItem.XmlData = rssNode.InnerXml.ToString();

                    if (!string.IsNullOrWhiteSpace(feedItem.Link))
                    {
                        items.Add(feedItem);
                    }
                }
            }
            catch (Exception ex)
            {
                currentEx = ex;
            }

            error = currentEx;
            return items;
        }
    }
   
}
