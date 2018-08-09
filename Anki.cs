using AnkiSharp.Helpers;
using AnkiSharp.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Linq;
using System.Collections;

using Info = System.Tuple<string, string, AnkiSharp.Models.FieldList>;

namespace AnkiSharp
{
    public class Anki
    { 
        #region MEMBERS
        private SQLiteConnection _conn;

        private MediaInfo? _mediaInfo;

        private string _name;
        private Assembly _assembly;
        private string _path;
        private string _collectionFilePath;

        private List<AnkiItem> _ankiItems;
        private Queue<CardMetadata> _cardsMetadatas;
        private List<RevLogMetadata> _revLogMetadatas;
        
        /// <summary>
        /// Key : string which represent Mid
        /// Value : Tuple string, string, FieldList which represent repectively the format, the css and the field list
        /// </summary>
        OrderedDictionary _infoPerMid;
        #endregion
        
        #region CTOR
        /// <summary>
        /// Creates a Anki object
        /// </summary>
        /// <param name="name">Specify the name of apkg file and deck</param>
        /// <param name="path">Where to save your apkg file</param>
        public Anki(string name, MediaInfo? info = null, string path = null)
        {
            _cardsMetadatas = new Queue<CardMetadata>();
            _revLogMetadatas = new List<RevLogMetadata>();

            _assembly = Assembly.GetExecutingAssembly();

            _mediaInfo = info;

            if (path == null)
                _path = Path.Combine(Path.GetDirectoryName(_assembly.Location), "tmp");
            else
                _path = path;

            if (Directory.Exists(_path) == false)
                Directory.CreateDirectory(_path);
            
            Init(_path, name);
        }

        /// <summary>
        /// Create anki object from an Apkg file
        /// </summary>
        /// <param name="name">Specify the name of apkg file and deck</param>
        /// <param name="file">Apkg file</param>
        public Anki(string name, ApkgFile file, MediaInfo? info = null)
        {
            _cardsMetadatas = new Queue<CardMetadata>();
            _revLogMetadatas = new List<RevLogMetadata>();

            _assembly = Assembly.GetExecutingAssembly();
            _path = Path.Combine(Path.GetDirectoryName(_assembly.Location), "tmp");

            _mediaInfo = info;

            if (Directory.Exists(_path) == false)
                Directory.CreateDirectory(_path);

            Init(_path, name);
            
            _collectionFilePath = Path.Combine(_path, "collection.db");

            ReadApkgFile(file.Path());
        }
        #endregion

        #region FUNCTIONS

        #region SETTERS
        public void SetFields(params string[] values)
        {
            FieldList fields = new FieldList();

            foreach (var value in values)
            {
                fields.Add(new Field(value));
            }

            var currentDefault = _infoPerMid["DEFAULT"] as Info;
            var newDefault = new Info(currentDefault.Item1, currentDefault.Item2, fields);

            _infoPerMid["DEFAULT"] = newDefault;
        }

        public void SetCss(string filepath)
        {
            var css = new StreamReader(filepath).ReadToEnd();
            var currentDefault = _infoPerMid["DEFAULT"] as Info;
            var newDefault = new Info(currentDefault.Item1, css, currentDefault.Item3);

            _infoPerMid["DEFAULT"] = newDefault;
        }

        public void SetFormat(string format)
        {
            var currentDefault = _infoPerMid["DEFAULT"] as Info;
            var newDefault = new Info(format, currentDefault.Item2, currentDefault.Item3);

            _infoPerMid["DEFAULT"] = newDefault;
        }
        #endregion

        #region PUBLIC
        /// <summary>
        /// Create a apkg file with all the words
        /// </summary>
        public void CreateApkgFile(string path)
        {
            _collectionFilePath = Path.Combine(_path, "collection.db");

            if (File.Exists(_collectionFilePath) == true)
                File.Delete(_collectionFilePath);

            File.Create(_collectionFilePath).Close();

            CreateMediaFile();

            ExecuteSQLiteCommands();

            CreateZipFile(path);
        }
        
        /// <summary>
        /// Creates an AnkiItem and add it to the Anki object
        /// </summary>
        public void AddItem(params string[] properties)
        {
            var mid = "";
            IDictionaryEnumerator myEnumerator = _infoPerMid.GetEnumerator();

            while (myEnumerator.MoveNext())
            {
                if (IsRightFieldList((myEnumerator.Value as Info).Item3, properties))
                {
                    mid = myEnumerator.Key.ToString();
                    break;
                }   
            }

            if (_infoPerMid.Contains(mid) && properties.Length != (_infoPerMid[mid] as Info).Item3.Count)
                throw new ArgumentException("Number of fields provided is not the same as the one expected");

            AnkiItem item = new AnkiItem((_infoPerMid[mid] as Info).Item3, properties)
            {
                Mid = mid
            };

            if (ContainsItem(item) == true)
                return;

            _ankiItems.Add(item);
        }

        /// <summary>
        /// Add AnkiItem to the Anki object
        /// </summary>
        public void AddItem(AnkiItem item)
        {
            if (item.Mid == "")
                item.Mid = "DEFAULT";

            if (_infoPerMid.Contains(item.Mid) && item.Count != (_infoPerMid[item.Mid] as Info).Item3.Count)
                throw new ArgumentException("Number of fields provided is not the same as the one expected");
            else if (ContainsItem(item) == true)
                return;

            _ankiItems.Add(item);
        }

        /// <summary>
        /// Tell if the anki object contains an AnkiItem (strict comparison)
        /// </summary>
        public bool ContainsItem(AnkiItem item)
        {
            int matching = 1;
            
            foreach (var ankiItem in _ankiItems)
            {
                if (item == ankiItem)
                    ++matching;
            }

            return matching == item.Count ? true : false;
        }

        /// <summary>
        /// Tell if the anki object contains an AnkiItem (user defined comparison)
        /// </summary>
        public bool ContainsItem(Func<AnkiItem, bool> comparison)
        {
            foreach (var ankiItem in _ankiItems)
            {
                if (comparison(ankiItem))
                    return true;
            }
            
            return false;
        }

        public AnkiItem CreateAnkiItem(params string[] properties)
        {
            FieldList list = null;
            IDictionaryEnumerator myEnumerator = _infoPerMid.GetEnumerator();

            while (myEnumerator.MoveNext())
            {
                if (IsRightFieldList((myEnumerator.Value as Info).Item3, properties))
                {
                    list = (myEnumerator.Value as Info).Item3;
                    break;
                }
            }
            
            return new AnkiItem(list, properties);
        }

        #endregion

        #region PRIVATE

        private void Init(string path, string name)
        {
            _infoPerMid = new OrderedDictionary();
            _name = name;
            _ankiItems = new List<AnkiItem>();
            _assembly = Assembly.GetExecutingAssembly();

            _path = path;

            var css = GeneralHelper.ReadResource("AnkiSharp.AnkiData.CardStyle.css");
            var fields = new FieldList
            {
                new Field("FrontSide"),
                new Field("Back")
            };

            _infoPerMid.Add("DEFAULT", new Info("", css, fields));
        }

        private bool IsRightFieldList(FieldList list, string[] properties)
        {
            if (list.Count != properties.Length)
                return false;

            return true;
        }

        private void CreateZipFile(string path)
        {
            string anki2FilePath = Path.Combine(_path, "collection.anki2");
            string mediaFilePath = Path.Combine(_path, "media");

            File.Move(_collectionFilePath, anki2FilePath);
            string zipPath = Path.Combine(path, _name + ".apkg");

            if (File.Exists(zipPath) == true)
                File.Delete(zipPath);

            ZipFile.CreateFromDirectory(_path, zipPath);

            File.Delete(anki2FilePath);
            File.Delete(mediaFilePath);

            int i = 0;
            string currentFile = Path.Combine(_path, i.ToString());

            while (File.Exists(currentFile))
            {
                File.Delete(currentFile);
                ++i;
                currentFile = Path.Combine(_path, i.ToString());
            }

        }

        private double CreateCol()
        {
            var timeStamp = GeneralHelper.GetTimeStampTruncated();
            var crt = GeneralHelper.GetTimeStampTruncated();

            string dir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            var confFileContent = GeneralHelper.ReadResource("AnkiSharp.AnkiData.conf.json");
            var conf = confFileContent.Replace("{MODEL}", timeStamp.ToString()).Replace("\r\n", "");

            var id_deck = GeneralHelper.GetTimeStampTruncated();

            var modelsFileContent = GeneralHelper.ReadResource("AnkiSharp.AnkiData.models.json");

            StringBuilder models = new StringBuilder();

            foreach (var key in _infoPerMid.Keys.Cast<string>().ToList())
            {
                var obj = (_infoPerMid[key] as Info);

                if (models.Length > 0)
                    models.Append(", ");

                if (key.ToString() == "DEFAULT")
                {
                    var newMid = GeneralHelper.GetTimeStampTruncated().ToString();
                    var newEntry = _infoPerMid["DEFAULT"];

                    _infoPerMid.Add(newMid, newEntry);
                    _ankiItems.ForEach(x => x.Mid = x.Mid == "DEFAULT" ? newMid : x.Mid);
                    models.Append(modelsFileContent.Replace("{MID}", newMid));
                }
                else
                    models.Append(modelsFileContent.Replace("{MID}", key as string));

                models = models.Replace("{CSS}", obj.Item2);
                models = models.Replace("{ID_DECK}", id_deck.ToString());

                var json = obj.Item3.ToJSON();
                models = models.Replace("{FLDS}", json);

                var format = obj.Item1 != "" ? obj.Item3.Format(obj.Item1) : obj.Item3.ToString();

                var qfmt = Regex.Split(format, "<hr id=answer(.*?)>|<br>")[0];
                var afmt = format;

                models = models.Replace("{QFMT}", qfmt).Replace("{AFMT}", afmt).Replace("\r\n", "");
            }

            var deckFileContent = GeneralHelper.ReadResource("AnkiSharp.AnkiData.decks.json");
            var deck = deckFileContent.Replace("{NAME}", _name).Replace("{ID_DECK}", id_deck.ToString()).Replace("\r\n", "");

            var dconfFileContent = GeneralHelper.ReadResource("AnkiSharp.AnkiData.dconf.json");
            var dconf = dconfFileContent.Replace("\r\n", "");

            string insertCol = "INSERT INTO col VALUES(1, " + crt + ", " + timeStamp + ", " + timeStamp + ", 11, 0, 0, 0, '" + conf + "', '{" + models.ToString() + "}', '" + deck + "', '" + dconf + "', " + "'{}'" + ");";

            SQLiteHelper.ExecuteSQLiteCommand(_conn, insertCol);

            return id_deck;
        }

        private void CreateNotesAndCards(double id_deck)
        {
            foreach (var ankiItem in _ankiItems)
            {
                var fields = (_infoPerMid[ankiItem.Mid] as Info).Item3;
                var id_note = GeneralHelper.GetTimeStampTruncated();
                var guid = ((ShortGuid)Guid.NewGuid()).ToString().Substring(0, 10);
                var mid = ankiItem.Mid;
                var mod = GeneralHelper.GetTimeStampTruncated();

                var flds = "";
                if (_mediaInfo != null)
                    flds = GeneralHelper.ConcatFields(fields, ankiItem, "\x1f", _mediaInfo.Value.field);
                else
                    flds = GeneralHelper.ConcatFields(fields, ankiItem, "\x1f");

                string sfld = ankiItem[fields[0].Name].ToString();
                var csum = "";

                using (SHA1Managed sha1 = new SHA1Managed())
                {
                    var l = sfld.Length >= 9 ? 8 : sfld.Length;
                    var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(sfld));
                    var sb = new StringBuilder(hash.Length);

                    foreach (byte b in hash)
                    {
                        sb.Append(b.ToString());
                    }

                    csum = sb.ToString().Substring(0, 10);
                }

                string insertNote = "INSERT INTO notes VALUES(" + id_note + ", '" + guid + "', " + mid + ", " + mod + ", -1, '  ', '" + flds + "', '" + sfld + "', " + csum + ", 0, '');";
                SQLiteHelper.ExecuteSQLiteCommand(_conn, insertNote);

                var id_card = GeneralHelper.GetTimeStampTruncated();
                string insertCard = "";

                if (_cardsMetadatas.Count != 0)
                {
                    CardMetadata metadata = _cardsMetadatas.Dequeue();
                    insertCard = "INSERT INTO cards VALUES(" + id_card + ", " + id_note + ", " + id_deck + ", " + "0, " + mod + ", -1, " + metadata.type + ", " + metadata.queue + ", " + metadata.due + ", " + metadata.ivl + ", " + metadata.factor + ", " + metadata.reps + ", " + metadata.lapses + ", " + metadata.left + ", " + metadata.odue + ", " + metadata.odid + ", 0, '');";
                }
                else
                    insertCard = "INSERT INTO cards VALUES(" + id_card + ", " + id_note + ", " + id_deck + ", " + "0, " + mod + ", -1, 0, 0, " + id_note + ", 0, 0, 0, 0, 0, 0, 0, 0, '');";

                SQLiteHelper.ExecuteSQLiteCommand(_conn, insertCard);
            }
        }

        private void AddRevlogMetadata()
        {
            if (_revLogMetadatas.Count != 0)
            {
                string insertRevLog = "";

                foreach (var revlogMetadata in _revLogMetadatas)
                {
                    insertRevLog = "INSERT INTO revlog VALUES(" + revlogMetadata.id + ", " + revlogMetadata.cid + ", " + revlogMetadata.usn + ", " + revlogMetadata.ease + ", " + revlogMetadata.ivl + ", " + revlogMetadata.lastIvl + ", " + revlogMetadata.factor + ", " + revlogMetadata.time + ", " + revlogMetadata.type + ")";

                    SQLiteHelper.ExecuteSQLiteCommand(_conn, insertRevLog);
                }
            }
        }

        private void ExecuteSQLiteCommands()
        {
            _conn = new SQLiteConnection(@"Data Source=" + _collectionFilePath + ";Version=3;");
            try
            {
                _conn.Open();

                var column = GeneralHelper.ReadResource("AnkiSharp.SqLiteCommands.ColumnTable.txt");
                var notes = GeneralHelper.ReadResource("AnkiSharp.SqLiteCommands.NotesTable.txt");
                var cards = GeneralHelper.ReadResource("AnkiSharp.SqLiteCommands.CardsTable.txt");
                var revLogs = GeneralHelper.ReadResource("AnkiSharp.SqLiteCommands.RevLogTable.txt");
                var graves = GeneralHelper.ReadResource("AnkiSharp.SqLiteCommands.GravesTable.txt");
                var indexes = GeneralHelper.ReadResource("AnkiSharp.SqLiteCommands.Indexes.txt");

                SQLiteHelper.ExecuteSQLiteCommand(_conn, column);
                SQLiteHelper.ExecuteSQLiteCommand(_conn, notes);
                SQLiteHelper.ExecuteSQLiteCommand(_conn, cards);
                SQLiteHelper.ExecuteSQLiteCommand(_conn, revLogs);
                SQLiteHelper.ExecuteSQLiteCommand(_conn, graves);
                SQLiteHelper.ExecuteSQLiteCommand(_conn, indexes);

                var id_deck = CreateCol();
                CreateNotesAndCards(id_deck);

                AddRevlogMetadata();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            finally
            {
                _conn.Close();
                _conn.Dispose();
                SQLiteConnection.ClearAllPools();
            }
        }
        
        private void CreateMediaFile()
        {
            string mediaFilePath = Path.Combine(_path, "media");

            if (File.Exists(mediaFilePath))
                File.Delete(mediaFilePath);

            using (FileStream fs = File.Create(mediaFilePath))
            {
                string data = "{";
                int i = 0;

                if (_mediaInfo != null)
                {
                    foreach (var selectedWord in _ankiItems)
                    {
                        SynthetizerHelper.CreateAudio(Path.Combine(_path, i.ToString()), selectedWord[_mediaInfo.Value.field].ToString(), _mediaInfo.Value.cultureInfo);

                        data += "\"" + i.ToString() + "\": \"" + selectedWord[_mediaInfo.Value.field] + ".wav\"";

                        if (i < _ankiItems.Count() - 1)
                            data += ", ";

                        i++;
                    }
                }
                data += "}";

                Byte[] info = new UTF8Encoding(true).GetBytes(data);
                fs.Write(info, 0, info.Length);
                fs.Close();
            }
        }

        public void ReadApkgFile(string path)
        {
            if (File.Exists(Path.Combine(_path, "collection.db")))
                File.Delete(Path.Combine(_path, "collection.db"));

            if (File.Exists(Path.Combine(_path, "media")))
                File.Delete(Path.Combine(_path, "media"));

            ZipFile.ExtractToDirectory(path, _path);

            string anki2File = Path.Combine(_path, "collection.anki2");
            
            File.Move(anki2File, _collectionFilePath);

            _conn = new SQLiteConnection(@"Data Source=" + _collectionFilePath + ";Version=3;");

            try
            {
                _conn.Open();

                Mapper mapper = Mapper.Instance;

                var cardMetadatas = Mapper.MapSQLiteReader(_conn, "SELECT cards.type, cards.queue, cards.due, cards.ivl, cards.factor, cards.reps, cards.lapses, cards.left, cards.odue, cards.odid FROM notes, cards WHERE cards.nid == notes.id;");

                foreach (var cardMetadata in cardMetadatas)
                {
                    _cardsMetadatas.Enqueue(cardMetadata.ToObject<CardMetadata>());
                }

                SQLiteDataReader reader = SQLiteHelper.ExecuteSQLiteCommandRead(_conn, "SELECT notes.flds, notes.mid FROM notes");
                List<double> mids = new List<double>();
                string[] splitted = null;
                List<string[]> result = new List<string[]>();
                
                while (reader.Read())
                {
                    splitted = reader.GetString(0).Split('\x1f');

                    var currentMid = reader.GetInt64(1);
                    if (mids.Contains(currentMid) == false)
                        mids.Add(currentMid);

                    result.Add(splitted);
                }

                reader.Close();
                reader = SQLiteHelper.ExecuteSQLiteCommandRead(_conn, "SELECT models FROM col");
                JObject models = null;

                while (reader.Read())
                {
                    models = JObject.Parse(reader.GetString(0));
                }

                var regex = new Regex("{{(.*?)}}");
                
                foreach (var mid in mids)
                {
                    var afmt = models["" + mid]["tmpls"].First["afmt"].ToString();
                    var css = models["" + mid]["css"].ToString();

                    var matches = regex.Matches(afmt);
                    FieldList fields = new FieldList();
                    
                    foreach (Match match in matches)
                    {
                        fields.Add(new Field(match.Value.Replace("{{", "").Replace("}}", "")));
                    }

                    _infoPerMid.Add("" + mid, new Info(afmt.Replace("\n", "\\n"), css.Replace("\n", "\\n"), fields));
                }

                reader.Close();

                var revLogMetadatas = Mapper.MapSQLiteReader(_conn, "SELECT * FROM revlog");

                foreach (var revLogMetadata in revLogMetadatas)
                {
                    _revLogMetadatas.Add(revLogMetadata.ToObject<RevLogMetadata>());
                }

                foreach (var res in result)
                {
                    AddItem(res);
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            finally
            {
                _conn.Close();
                _conn.Dispose();
                SQLiteConnection.ClearAllPools();
            }
        }

        #endregion

        #endregion
    }
}
