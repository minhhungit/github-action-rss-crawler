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

        static object syncRoot = "";
        public void WriteTextAsync(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                lock (syncRoot)
                {
                    using (StreamWriter w = new StreamWriter(this.FilePath, true))
                    {
                        w.WriteLineAsync(text);
                    }
                }
            }
        }

        public void Info(string text)
        {
            WriteTextAsync($"INFO\t{text}");
        }

        public void Error(string text)
        {
            WriteTextAsync($"ERROR\t{text}");
        }
    }
}
