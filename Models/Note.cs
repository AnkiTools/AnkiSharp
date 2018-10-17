using AnkiSharp.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

using Info = System.Tuple<string, string, AnkiSharp.Models.FieldList>;

namespace AnkiSharp.Models
{
    internal class Note
    {
        string _guid;
        string _mid;
        string _mod;
        string _usn;
        string _tags;
        string _flds;
        string _sfld;
        string _csum;
        string _flags;
        string _data;
        
        internal long Id { private set; get; }
        internal string Query { private set; get; }

        public Note(OrderedDictionary infoPerMid, MediaInfo mediaInfo, AnkiItem ankiItem)
        {
            var fields = (infoPerMid[ankiItem.Mid] as Info).Item3;
            Id = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            _guid = ((ShortGuid)Guid.NewGuid()).ToString().Substring(0, 10);
            _mid = ankiItem.Mid;

           
            _mod = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();

            _flds = GeneralHelper.ConcatFields(fields, ankiItem, "\x1f", mediaInfo);

            _sfld = ankiItem[fields[0].Name].ToString();
            var csum = GeneralHelper.CheckSum(_sfld);
            
            Query = "INSERT INTO notes VALUES(" + Id + ", '" + _guid + "', " + _mid + ", " + _mod + ", -1, '  ', '" + _flds + "', '" + _sfld + "', " + csum + ", 0, '');";
        }
    }
}
