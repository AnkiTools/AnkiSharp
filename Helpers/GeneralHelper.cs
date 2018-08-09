using AnkiSharp.Models;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

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

        internal static Double GetTimeStampTruncated()
        {
            return Math.Truncate(DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds);
        }

        internal static string ReadResource(string path)
        {
            return new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(path)).ReadToEnd();
        }

    }
}
