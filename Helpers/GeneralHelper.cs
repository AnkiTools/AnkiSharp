using AnkiSharp.Models;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace AnkiSharp.Helpers
{
    internal static class GeneralHelper
    {
        internal static string ConcatFields(FieldList flds, AnkiItem item, string separator, string fieldForSound = null)
        {
            var matchedFields = (from t in flds
                                select item[t.Name]).ToArray();

            if (fieldForSound != null)
            {
                int indexOfField = Array.IndexOf(matchedFields, item[fieldForSound]);

                if (indexOfField != -1)
                    matchedFields[indexOfField] += "[sound:" + matchedFields[0] + ".wav]";
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
