using AnkiSharp.Helpers;
using AnkiSharp.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SQLite;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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

            if (mid == "" || (_infoPerMid.Contains(mid) && properties.Length != (_infoPerMid[mid] as Info).Item3.Count))
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
                new Field("Front"),
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
        
        private string CreateCol()
        {
            Collection collection = new Collection(_infoPerMid, _ankiItems, _name);

            SQLiteHelper.ExecuteSQLiteCommand(_conn, collection.Query);

            return collection.DeckId;
        }

        private void CreateNotesAndCards(string id_deck)
        {
            foreach (var ankiItem in _ankiItems)
            {
                Note note = new Note(_infoPerMid, _mediaInfo, ankiItem);

                SQLiteHelper.ExecuteSQLiteCommand(_conn, note.Query);

                Card card = new Card(_cardsMetadatas, note, id_deck);

                SQLiteHelper.ExecuteSQLiteCommand(_conn, card.Query);
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

                byte[] info = new UTF8Encoding(true).GetBytes(data);
                fs.Write(info, 0, info.Length);
                fs.Close();
            }
        }

        private void ReadApkgFile(string path)
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

                var cardMetadatas = Mapper.MapSQLiteReader(_conn, "SELECT id, mod, type, queue, due, ivl, factor, reps, lapses, left, odue, odid FROM cards");

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
                
                AddFields(models, mids);
                
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
        
        private void AddFields(JObject models, List<double> mids)
        {
            var regex = new Regex("{{hint:(.*?)}}|{{(.*?)}}");

            foreach (var mid in mids)
            {
                var qfmt = models["" + mid]["tmpls"].First["qfmt"].ToString().Replace("\"", "");
                var afmt = models["" + mid]["tmpls"].First["afmt"].ToString();
                var css = models["" + mid]["css"].ToString();

                afmt = afmt.Replace("{{FrontSide}}", qfmt);

                var matches = regex.Matches(afmt);
                FieldList fields = new FieldList();

                foreach (Match match in matches)
                {
                    var value = match.Value.Replace("hint:", "");
                    var field = new Field(value.Replace("{{", "").Replace("}}", ""));

                    fields.Add(field);
                }

                _infoPerMid.Add("" + mid, new Info(afmt.Replace("\n", "\\n"), css.Replace("\n", "\\n"), fields));
            }
        }
        
        #endregion

        #endregion
    }
}
