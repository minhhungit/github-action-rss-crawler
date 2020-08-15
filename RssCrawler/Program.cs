using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace RssCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            var appRootPath = EnvironmentHelper.GetApplicationRoot();
            WriteAllText(Path.Combine(appRootPath, "../", "README.md"), "Hello From Jin -- this file is auto committed " + DateTime.Now);

            RssCrawler.Crawl();

            //string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //string crawlLogFolderPath= Path.Combine(assemblyFolder, "Logs");

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