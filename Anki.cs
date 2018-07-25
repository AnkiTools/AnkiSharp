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
        
        private FieldList _flds;
        private string _css;
        private string _format;
        #endregion

        #region CTOR
        /// <summary>
        /// Creates a Anki object
        /// </summary>
        /// <param name="path">Where to save your apkg file</param>
        /// <param name="name">Specify the name of apkg file and deck</param>
        public Anki(string name, string path = null)
        {
            _assembly = Assembly.GetExecutingAssembly();

            if (path == null)
                _path = Path.Combine(Path.GetDirectoryName(_assembly.Location), "tmp");
            else
                _path = path;

            if (Directory.Exists(_path) == false)
                Directory.CreateDirectory(_path);
            
            Init(_path, name);
        }

        public Anki(ApkgFile file)
        {
            _assembly = Assembly.GetExecutingAssembly();
            _path = Path.Combine(Path.GetDirectoryName(_assembly.Location), "tmp");

            if (Directory.Exists(_path) == false)
                Directory.CreateDirectory(_path);

            Init(_path, Path.GetFileName(file.Path()).Replace(".apkg", "_copy"));

            _collectionFilePath = Path.Combine(_path, "collection.db");

            ReadApkgFile(file.Path());
        }
        #endregion

        #region FUNCTIONS

        #region SETTERS
        public void SetFields(params string[] values)
        {
            _flds.Clear();
            foreach (var value in values)
            {
                _flds.Add(new Field(value));
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
            if (properties.Length != _flds.Count)
                throw new ArgumentException("Number of fields provided is not the same as the one expected");

            _ankiItems.Add(new AnkiItem(_flds, properties));
        }

        #endregion

        #region PRIVATE
        private void Init(string path, string name)
        {
            _name = name;
            _ankiItems = new List<AnkiItem>();
            _assembly = Assembly.GetExecutingAssembly();

            _path = path;

            _flds = new FieldList
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

            var json = _flds.ToJSON();
            models = models.Replace("{FLDS}", json);

            var format = _format != null ? _flds.Format(_format) : _flds.ToString();
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
                var flds = GeneralHelper.ConcatFields(_flds, ankiItem, "\x1f");
                string sfld = ankiItem[_flds[0].Name].ToString();
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
                string insertCard = "INSERT INTO cards VALUES(" + id_card + ", " + id_note + ", " + id_deck + ", " + "0, " + mod + ", -1, 0, 0, " + id_note + ", 0, 0, 0, 0, 0, 0, 0, 0, '');";

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

                SQLiteHelper.ExecuteSQLiteCommand(_conn, column);
                SQLiteHelper.ExecuteSQLiteCommand(_conn, notes);
                SQLiteHelper.ExecuteSQLiteCommand(_conn, cards);
                SQLiteHelper.ExecuteSQLiteCommand(_conn, revLogs);
                SQLiteHelper.ExecuteSQLiteCommand(_conn, graves);
                
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
                    //SynthetizerHelper.PlaySound(_path + @"\", selectedWord.Word.PinYin, selectedWord.Word.Simplified, i.ToString());

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
                SQLiteDataReader reader = SQLiteHelper.ExecuteSQLiteCommandRead(_conn, "SELECT flds, mid FROM notes");
                var mid = -1.0;
                string[] splitted = null;
                List<string[]> result = new List<string[]>();

                while (reader.Read())
                {
                    splitted = reader.GetString(0).Split('\x1f');

                    mid = reader.GetInt64(1);
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
