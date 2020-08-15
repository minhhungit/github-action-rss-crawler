using LiteDB;
using RssCrawler.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace RssCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            var appRootPath = EnvironmentHelper.GetApplicationRoot();
            //WriteAllText(Path.Combine(appRootPath, "../", "README.md"), "Hello From Jin -- this file is auto committed " + DateTime.Now);

            RssCrawler.CrawlRss();

            string indexFilePath = Path.Combine(appRootPath, "../", "index.html");
            string indexContent = File.ReadAllText(indexFilePath, Encoding.UTF8);

            //var regex = new Regex(@"<!-- RSSDATA:START -->[\n\r]+(.*?)[\n\r]+<!-- RSSDATA:END -->");
            //var match = regex.Match(indexContent);
            //var result = match.Groups[1].Value;

            var change = string.Empty; //$"Hello, this text is auto generated {DateTime.Now:yyy/MM/dd HH:mm:ss}";

            var feedItems = SimpleFeedlyDatabaseAccess.GetAllFeedItems();

            ObjectId currentChannelId = null;

            var sb = new StringBuilder();
            var sbChannel = new StringBuilder();

            var counter = 1;
            foreach (var item in feedItems)
            {
                var isNewChannel = item.Channel.Id != currentChannelId;
                currentChannelId = item.Channel.Id;

                if (isNewChannel)
                {
                    counter = 1;
                    if (sbChannel.Length > 0) // has previous item
                    {
                        sbChannel.AppendLine("</div>"); // div.row
                        sb.Append(sbChannel);
                        sbChannel = new StringBuilder();
                    }

                    sbChannel.AppendLine($"<h2 class='channel-title'># <a href='{item.Channel.Link}' target='_blank'>{item.Channel.Title}</a></h2>");
                    sbChannel.AppendLine("<div class='row'>"); // new row
                }
                else
                {
                    if (counter == 6)
                    {
                        sbChannel.AppendLine("</div>");
                        sbChannel.AppendLine("<div class='row'>");
                    }                    
                }

                sbChannel.Append(@$"<div class='column'>
                                <div class='card'>
                                  <div class='feed-img-wrapper'><img src='{item.CoverImageUrl}' /></div>
                                  <div class='feed-title'>{item.Title}</div>
                                </div>
                              </div>");

                counter++;
            }

            sb.AppendLine("</div>"); // div.row

            change = sb.ToString();

            //var newContent = Regex.Replace(indexContent, $"<!-- RSSDATA:START -->[\n\r]+(.*?)[\n\r]+<!-- RSSDATA:END -->", string.Format("<!-- RSSDATA:START -->\n{0}\n<!-- RSSDATA:END -->", change));
            var newContent = Regex.Replace(indexContent, $"<!-- RSSDATA:START -->(?:[^\n]*(\n+))+<!-- RSSDATA:END -->", string.Format("<!-- RSSDATA:START -->\n{0}\n<!-- RSSDATA:END -->", change));
            WriteAllText(indexFilePath, newContent);

            //string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //string crawlLogFolderPath= Path.Combine(assemblyFolder, "Logs");

            //if (!Directory.Exists(crawlLogFolderPath))
            //{
            //    Directory.CreateDirectory(crawlLogFolderPath);
            //}
            //string[] filePaths = Directory.GetFiles(crawlLogFolderPath);
            //foreach (var filename in filePaths)
            //{
            //    string targetFolderPath = Path.Combine(appRootPath, "..\\Logs\\");
            //    if (!Directory.Exists(targetFolderPath))
            //    {
            //        Directory.CreateDirectory(targetFolderPath);
            //    }

            //    var targetFilePath = Path.Combine(targetFolderPath, Path.GetFileName(filename));
            //    File.Copy(filename, targetFilePath, true);
            //}
        }

        static void WriteAllText(string path, string txt)
        {
            var bytes = Encoding.UTF8.GetBytes(txt);
            using (var f = File.Open(path, FileMode.Create))
            {
                f.Write(bytes, 0, bytes.Length);
            }
        }
    }
}