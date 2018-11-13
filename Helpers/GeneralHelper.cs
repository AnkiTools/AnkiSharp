using AnkiSharp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace AnkiSharp.Helpers
{
    internal static class GeneralHelper
    {
        internal static Dictionary<string, string> extensionTag = new Dictionary<string, string>()
        {
            { ".wav", "[sound:{0}]" },
            { ".gif", "<img src=\"{0}\"/>" }
        };

        internal static string ConcatFields(FieldList flds, AnkiItem item, string separator, MediaInfo info)
        {
            var matchedFields = (from t in flds
                                 where item[t.Name] as string != ""
                                 select item[t.Name]).ToArray();

            if (info != null)
            {
                int indexOfField = Array.IndexOf(matchedFields, item[info.field]);

                if (indexOfField != -1)
                    matchedFields[indexOfField] += String.Format(extensionTag[info.extension], matchedFields[0] + info.extension);
            }
            
            return String.Join(separator, matchedFields);
        }
        
        internal static string ReadResource(string path)
        {
            return new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(path)).ReadToEnd();
        }

        internal static string CheckSum(string sfld)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var l = sfld.Length >= 9 ? 8 : sfld.Length;
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(sfld));
                var sb = new StringBuilder(hash.Length);

                foreach (byte b in hash)
                {
                    sb.Append(b.ToString());
                }

                return sb.ToString().Substring(0, 10);
            }
        }

    }
}
