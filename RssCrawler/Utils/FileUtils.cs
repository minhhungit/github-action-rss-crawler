using System.IO;

namespace RssCrawler.Utils
{
    public class FileUtils
    {
        static object syncRoot = "";
        public static void WriteText(string filePath, string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                lock (syncRoot)
                {
                    using (StreamWriter w = new StreamWriter(filePath, true))
                    {
                        w.WriteLineAsync(text).GetAwaiter().GetResult();
                    }
                }
            }
        }
    }
}
