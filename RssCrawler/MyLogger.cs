using RssCrawler.Utils;
using System.IO;

namespace RssCrawler
{
    public class MyLogger
    {
        private string FilePath { get; set; }

        public MyLogger(string path)
        {
            this.FilePath = path;
        }

        public void Info(string text)
        {
            FileUtils.WriteText(this.FilePath, $"INFO\t{text}");
        }

        public void Error(string text)
        {
            FileUtils.WriteText(this.FilePath, $"ERROR\t{text}");
        }
    }
}
