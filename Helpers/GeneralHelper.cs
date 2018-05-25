using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnkiSharp.Helpers
{
    internal static class GeneralHelper
    {
        internal static string ConcatFields(FieldList flds, AnkiItem item, string separator)
        {
            var test = from t in flds
                       select item[t.Name];

            return String.Join(separator, test.ToArray());
        }

        internal static Double GetTimeStampTruncated()
        {
            return Math.Truncate(DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds);
        }

    }
}
