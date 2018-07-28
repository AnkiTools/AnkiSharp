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

namespace AnkiSharp
{
    public class Anki
    { 
        #region FIELDS
        private SQLiteConnection _conn;

        private string _name;
        private Assembly _assembly;
        private string _path;
        private string _collectionFilePath;

        private List<AnkiItem> _ankiItems;
        private Queue<CardMetadata> _metadatas;
        private string _css;
        private string _format;
        #endregion

        #region PROPERTIES

        public FieldList Fields { get; private set; }

        #endregion

        #region CTOR
        /// <summary>
        /// Creates a Anki object
        /// </summary>
        /// <param name="path">Where to save your apkg file</param>
        /// <param name="name">Specify the name of apkg file and deck</param>
        public Anki(string name, string path = null)
        {
            _metadatas = new Queue<CardMetadata>();

            _assembly = Assembly.GetExecutingAssembly();

            if (path == null)
                _path = Path.Combine(Path.GetDirectoryName(_assembly.Location), "tmp");
            else
                _path = path;

            if (Directory.Exists(_path) == false)
                Directory.CreateDirectory(_path);
            
            Init(_path, name);
        }

        public Anki(string name, ApkgFile file)
        {
            _metadatas = new Queue<CardMetadata>();

            _assembly = Assembly.GetExecutingAssembly();
            _path = Path.Combine(Path.GetDirectoryName(_assembly.Location), "tmp");

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
            Fields.Clear();
            foreach (var value in values)
            {
                Fields.Add(new Field(value));
            }
        }

        public void SetCss(string filepath)
        {
            _css = new StreamReader(filepath).ReadToEnd();
        }

        public void SetFormat(string format)
        {
            _format = format;
        }
        #endregion

        #region PUBLIC
        /// <summary>
        /// Create a apkg file with all the words
        /// </summary>
        public void CreateApkgFile(string path)
        {
            _collectionFilePath = Path.Combine(_path, "collection.db");
            File.Create(_collectionFilePath).Close();

            CreateMediaFile();

            ExecuteSQLiteCommands();

            CreateZipFile(path);
        }
        
        public void AddItem(params string[] properties)
        {
            if (properties.Length != Fields.Count)
                throw new ArgumentException("Number of fields provided is not the same as the one expected");

            AnkiItem item = new AnkiItem(Fields, properties);

            if (ContainsItem(item) == true)
                return;

            _ankiItems.Add(item);
        }

        public void AddItem(AnkiItem item)
        {
            if (item.Count != Fields.Count)
                throw new ArgumentException("Number of fields provided is not the same as the one expected");
            else if (ContainsItem(item) == true)
                return;

            _ankiItems.Add(item);
        }

        public bool ContainsItem(AnkiItem item)
        {
            foreach (var ankiItem in _ankiItems)
            {
                if (item == ankiItem)
                    return true;
            }

            return false;
        }
        #endregion

        #region PRIVATE
        private void Init(string path, string name)
        {
            _name = name;
            _ankiItems = new List<AnkiItem>();
            _assembly = Assembly.GetExecutingAssembly();

            _path = path;

            Fields = new FieldList
            {
                new Field("Front"),
                new Field("Back")
            };

            _css = new StreamReader(_assembly.GetManifestResourceStream("AnkiSharp.AnkiData.CardStyle.css")).ReadToEnd();
        }

        private void CreateZipFile(string path)
        {
            string anki2FilePath = Path.Combine(_path, "collection.anki2");
            string mediaFilePath = Path.Combine(_path, "media");

            File.Move(_collectionFilePath, anki2FilePath);
            string zipPath = Path.Combine(path, _name + ".apkg");

            ZipFile.CreateFromDirectory(_path, zipPath);

            File.Delete(anki2FilePath);
            File.Delete(mediaFilePath);
        }

        private double CreateCol()
        {
            var timeStamp = GeneralHelper.GetTimeStampTruncated();
            var crt = GeneralHelper.GetTimeStampTruncated();

            string dir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            var confFileContent = new StreamReader(_assembly.GetManifestResourceStream("AnkiSharp.AnkiData.conf.json")).ReadToEnd();
            var conf = confFileContent.Replace("{MODEL}", timeStamp.ToString()).Replace("\r\n", "");

            var id_deck = GeneralHelper.GetTimeStampTruncated();

            var modelsFileContent = new StreamReader(_assembly.GetManifestResourceStream("AnkiSharp.AnkiData.models.json")).ReadToEnd();
            var models = modelsFileContent.Replace("{CSS}", _css);
            models = models.Replace("{ID_DECK}", id_deck.ToString());

            var json = Fields.ToJSON();
            models = models.Replace("{FLDS}", json);

            var format = _format != null ? Fields.Format(_format) : Fields.ToString();
            var qfmt = Regex.Split(format, "<br>")[0];
            var afmt = format;
            
            models = models.Replace("{QFMT}", qfmt).Replace("{AFMT}", afmt).Replace("\r\n", "");
            
            var deckFileContent = new StreamReader(_assembly.GetManifestResourceStream("AnkiSharp.AnkiData.decks.json")).ReadToEnd();
            var deck = deckFileContent.Replace("{NAME}", _name).Replace("{ID_DECK}", id_deck.ToString()).Replace("\r\n", "");

            var dconfFileContent = new StreamReader(_assembly.GetManifestResourceStream("AnkiSharp.AnkiData.dconf.json")).ReadToEnd();
            var dconf = dconfFileContent.Replace("\r\n", "");

            string insertCol = "INSERT INTO col VALUES(1, " + crt + ", " + timeStamp + ", " + timeStamp + ", 11, 0, 0, 0, '" + conf + "', '" + models + "', '" + deck + "', '" + dconf + "', " + "'{}'" + ");";

            SQLiteHelper.ExecuteSQLiteCommand(_conn, insertCol);

            return id_deck;
        }

        private void CreateNotesAndCards(double id_deck)
        {
            foreach (var ankiItem in _ankiItems)
            {
                var id_note = GeneralHelper.GetTimeStampTruncated();
                var guid = ((ShortGuid)Guid.NewGuid()).ToString().Substring(0, 10);
                var mid = "1342697561419";
                var mod = GeneralHelper.GetTimeStampTruncated();
                var flds = GeneralHelper.ConcatFields(Fields, ankiItem, "\x1f");
                string sfld = ankiItem[Fields[0].Name].ToString();
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

                if (_metadatas.Count != 0)
                {
                    CardMetadata metadata = _metadatas.Dequeue();
                    insertCard = "INSERT INTO cards VALUES(" + id_card + ", " + id_note + ", " + id_deck + ", " + "0, " + mod + ", -1, " + metadata.type + ", " + metadata.queue + ", " + metadata.due + ", " + metadata.ivl + ", " + metadata.factor + ", " + metadata.reps + ", " + metadata.lapses + ", " + metadata.left + ", " + metadata.odue + ", " + metadata.odid + ", 0, '');";
                }
                else
                    insertCard = "INSERT INTO cards VALUES(" + id_card + ", " + id_note + ", " + id_deck + ", " + "0, " + mod + ", -1, 0, 0, " + id_note + ", 0, 0, 0, 0, 0, 0, 0, 0, '');";

                SQLiteHelper.ExecuteSQLiteCommand(_conn, insertCard);
            }
        }

        private void ExecuteSQLiteCommands()
        {
            _conn = new SQLiteConnection(@"Data Source=" + _collectionFilePath + ";Version=3;");
            try
            {
                _conn.Open();

                var column = new StreamReader(_assembly.GetManifestResourceStream("AnkiSharp.SqLiteCommands.ColumnTable.txt")).ReadToEnd();
                var notes = new StreamReader(_assembly.GetManifestResourceStream("AnkiSharp.SqLiteCommands.NotesTable.txt")).ReadToEnd();
                var cards = new StreamReader(_assembly.GetManifestResourceStream("AnkiSharp.SqLiteCommands.CardsTable.txt")).ReadToEnd();
                var revLogs = new StreamReader(_assembly.GetManifestResourceStream("AnkiSharp.SqLiteCommands.RevLogTable.txt")).ReadToEnd();
                var graves = new StreamReader(_assembly.GetManifestResourceStream("AnkiSharp.SqLiteCommands.GravesTable.txt")).ReadToEnd();
                var indexes = new StreamReader(_assembly.GetManifestResourceStream("AnkiSharp.SqLiteCommands.Indexes.txt")).ReadToEnd();

                SQLiteHelper.ExecuteSQLiteCommand(_conn, column);
                SQLiteHelper.ExecuteSQLiteCommand(_conn, notes);
                SQLiteHelper.ExecuteSQLiteCommand(_conn, cards);
                SQLiteHelper.ExecuteSQLiteCommand(_conn, revLogs);
                SQLiteHelper.ExecuteSQLiteCommand(_conn, graves);
                SQLiteHelper.ExecuteSQLiteCommand(_conn, indexes);

                var id_deck = CreateCol();
                CreateNotesAndCards(id_deck);
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
            using (FileStream fs = File.Create(mediaFilePath))
            {
                string data = "{";
                //int i = 0;

                //foreach (var selectedWord in _ankiItems)
                //{
                   // data += "\"" + i.ToString() + "\": \"" + selectedWord.Front + ".mp3\"";

                    //if (i < ankiItems.Count() - 1)
                    //    data += ", ";

                    //i++;
                //}
                data += "}";

                Byte[] info = new UTF8Encoding(true).GetBytes(data);
                fs.Write(info, 0, info.Length);
                fs.Close();
            }
        }

        public void ReadApkgFile(string path)
        {
            ZipFile.ExtractToDirectory(path, _path);

            string anki2File = Path.Combine(_path, "collection.anki2");
            
            File.Move(anki2File, _collectionFilePath);

            _conn = new SQLiteConnection(@"Data Source=" + _collectionFilePath + ";Version=3;");

            try
            {
                _conn.Open();
                SQLiteDataReader reader = SQLiteHelper.ExecuteSQLiteCommandRead(_conn, "SELECT notes.flds, notes.mid, cards.type, cards.queue, cards.due, cards.ivl, cards.factor, cards.reps, cards.lapses, cards.left, cards.odue, cards.odid FROM notes, cards WHERE cards.nid == notes.id;");
                var mid = -1.0;
                string[] splitted = null;
                List<string[]> result = new List<string[]>();
                CardMetadata metadata;

                while (reader.Read())
                {
                    splitted = reader.GetString(0).Split('\x1f');

                    mid = reader.GetInt64(1);
                    result.Add(splitted);

                    metadata = new CardMetadata
                    {
                        type = reader.GetInt64(2),
                        queue = reader.GetInt64(3),
                        due = reader.GetInt64(4),
                        ivl = reader.GetInt64(5),
                        factor = reader.GetInt64(6),
                        reps = reader.GetInt64(7),
                        lapses = reader.GetInt64(8),
                        left = reader.GetInt64(9),
                        odue = reader.GetInt64(10),
                        odid = reader.GetInt64(11)
                    };

                    _metadatas.Enqueue(metadata);
                }
                
                reader.Close();
                reader = SQLiteHelper.ExecuteSQLiteCommandRead(_conn, "SELECT models FROM col");
                JObject models = null;

                while (reader.Read())
                {
                    models = JObject.Parse(reader.GetString(0));
                }

                var regex = new Regex("{{(.*?)}}");
                var afmt = models["" + mid]["tmpls"].First["afmt"].ToString();
                var css = models["" + mid]["css"].ToString();
                var matches = regex.Matches(afmt);
                string[] fields = new string[matches.Count];

                int i = 0;

                foreach (Match match in matches)
                {
                    fields.SetValue(match.Value.Replace("{{", "").Replace("}}", ""), i);
                    ++i;
                }
                
                reader.Close();
                
                _css = css.Replace("\n", "\\n");
                SetFields(fields);
                SetFormat(afmt.Replace("\n", "\\n"));
                
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
