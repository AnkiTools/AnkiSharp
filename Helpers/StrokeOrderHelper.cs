using System;
using System.IO;
using System.Net;

namespace AnkiSharp.Helpers
{
    internal static class StrokeOrderHelper
    {
        internal static string baseUrl = "https://raw.githubusercontent.com/nmarley/chinese-char-animations/master/images/";

        internal static void DownloadImage(string path, string text)
        {
            var code = String.Format("U+{0:x4}", (int)text[0]).Replace("U+", "");
            var url = Path.Combine(baseUrl, code + ".gif");

            using (WebClient client = new WebClient())
            {
                client.DownloadFile(new Uri(url), path);
            }
        }
    }
}
