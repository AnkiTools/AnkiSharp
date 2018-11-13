using AnkiSharp.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Info = System.Tuple<string, string, AnkiSharp.Models.FieldList>;

namespace AnkiSharp.Models
{
    internal class Collection
    {
        long _id = 1;
        long _crt;
        string _mod;
        long _scm;
        long _ver;
        long _dty;
        long _usn;
        long _lastSync;
        string _conf;
        StringBuilder _models;
        string _decks;
        string _dconf;
        string _tags;

        internal string Query { private set; get; }
        internal string DeckId { private set; get; }
        
        public Collection(OrderedDictionary infoPerMid, List<AnkiItem> ankiItems, string name)
        {
            var mid = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();

            _mod = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
            _crt = GetDayStart();
            
            var confFileContent = GeneralHelper.ReadResource("AnkiSharp.AnkiData.conf.json");
            _conf = confFileContent.Replace("{MODEL}", mid).Replace("\r\n", "");

            DeckId = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();

            var modelsFileContent = GeneralHelper.ReadResource("AnkiSharp.AnkiData.models.json").Replace("{MOD}", _mod);

            _models = new StringBuilder();

            var alreadyAdded = new List<string>();

            foreach (var key in infoPerMid.Keys.Cast<string>().ToList())
            {
                var obj = (infoPerMid[key] as Info);

                if (alreadyAdded.Contains(obj.Item3.ToJSON().Replace("hint:", "").Replace("type:", "")))
                    continue;

                if (_models.Length > 0)
                    _models.Append(", ");

                if (key.ToString() == "DEFAULT")
                {
                    var newEntry = infoPerMid["DEFAULT"];

                    infoPerMid.Add(mid, newEntry);
                    ankiItems.ForEach(x => x.Mid = x.Mid == "DEFAULT" ? mid : x.Mid);
                    _models.Append(modelsFileContent.Replace("{MID}", mid));
                }
                else
                    _models.Append(modelsFileContent.Replace("{MID}", key as string));

                _models = _models.Replace("{CSS}", obj.Item2);
                _models = _models.Replace("{ID_DECK}", DeckId);

                var json = obj.Item3.ToJSON();
                
                _models = _models.Replace("{FLDS}", json.Replace("hint:", "").Replace("type:", ""));
                alreadyAdded.Add(json.Replace("hint:", "").Replace("type:", ""));

                var format = obj.Item1 != "" ? obj.Item3.Format(obj.Item1) : obj.Item3.ToFrontBack();

                var qfmt = Regex.Split(format, "<hr id=answer(.*?)>")[0];
                var afmt = format;

                afmt = afmt.Replace(qfmt, "{{FrontSide}}\\n");
                _models = _models.Replace("{QFMT}", qfmt).Replace("{AFMT}", afmt).Replace("\r\n", "");
            }

            var deckFileContent = GeneralHelper.ReadResource("AnkiSharp.AnkiData.decks.json");
            _decks = deckFileContent.Replace("{NAME}", name).Replace("{ID_DECK}", DeckId).Replace("{MOD}", _mod).Replace("\r\n", "");

            var dconfFileContent = GeneralHelper.ReadResource("AnkiSharp.AnkiData.dconf.json");
            _dconf = dconfFileContent.Replace("\r\n", "");

            Query = @"INSERT INTO col VALUES(" + _id + ", " + _crt + ", " + _mod + ", " + _mod + ", 11, 0, 0, 0, '"
                    + _conf + "', '{" + _models.ToString() + "}', '" + _decks + "', '" + _dconf + "', "
                    + "'{}'" + ");";
        }

        private long GetDayStart()
        {
            var dateOffset = DateTimeOffset.Now;
            TimeSpan FourHoursSpan = new TimeSpan(4, 0, 0);
            dateOffset = dateOffset.Subtract(FourHoursSpan);
            dateOffset = new DateTimeOffset(dateOffset.Year, dateOffset.Month, dateOffset.Day,
                                            0, 0, 0, dateOffset.Offset);
            dateOffset = dateOffset.Add(FourHoursSpan);
            return dateOffset.ToUnixTimeSeconds();
        }
    }
}
