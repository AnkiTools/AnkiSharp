using AnkiSharp.Helpers;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace AnkiSharp
{
    public class Anki
    { 
        #region FIELDS
        private SQLiteConnection _conn;

        private string _name;
        private Assembly _assembly;
        private string _path;
        private string _sqlitePath;
        private string _ankiDataPath;
        private string _collectionFilePath;

        private List<AnkiItem> _ankiItems;
        
        private FieldList _flds;
        #endregion

        #region CTOR
        /// <summary>
        /// Creates a Anki object
        /// </summary>
        /// <param name="path">Where to save your apkg file</param>
        /// <param name="name">Specify the name of apkg file and deck</param>
        public Anki(string path, string name)
        {
            _name = name;
            _ankiItems = new List<AnkiItem>();
            _assembly = Assembly.GetExecutingAssembly();
            _sqlitePath = _assembly.Location;
            _ankiDataPath = _assembly.Location;

            _path = path;

            _flds = new FieldList
            {
                new Field("Front"),
                new Field("Back")
            };
        }
        #endregion

        #region FUNCTIONS

        #region PUBLIC
        /// <summary>
        /// Create a apkg file with all the words
        /// </summary>
        public void CreateApkgFile()
        {
            _collectionFilePath = Path.Combine(_path, "collection.db");
            File.Create(_collectionFilePath).Close();

            CreateMediaFile();

            ExecuteSQLiteCommands();

            CreateZipFile();
        }

        public void SetFields(params string[] values)
        {
            _flds.Clear();
            foreach (var value in values)
            {
                _flds.Add(new Field(value));
            }
        }

        public void AddItem(params string[] properties)
        {
            if (properties.Length != _flds.Count)
                throw new ArgumentException("Number of fields provided is not the same as the one expected");

            _ankiItems.Add(new AnkiItem(_flds, properties));
        }

        #endregion

        #region PRIVATE
        private void CreateZipFile()
        {
            string anki2FilePath = Path.Combine(_path, "collection.anki2");
            string mediaFilePath = Path.Combine(_path, "media");

            File.Move(_collectionFilePath, anki2FilePath);

            string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string zipPath = Path.Combine(@"C:\Users\Clement\Desktop\", _name + ".apkg");

            ZipFile.CreateFromDirectory(_path, zipPath);

            File.Delete(anki2FilePath);
            File.Delete(mediaFilePath);
        }

        private Double GetTimeStampTruncated()
        {
            return Math.Truncate(DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds);
        }

        private double CreateCol()
        {
            var timeStamp = GetTimeStampTruncated();
            var crt = GetTimeStampTruncated();

            string dir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            var confFileContent = new StreamReader(_assembly.GetManifestResourceStream("AnkiSharp.AnkiData.conf.json")).ReadToEnd();
            var conf = confFileContent.Replace("{MODEL}", timeStamp.ToString()).Replace("\r\n", "");

            var id_deck = GetTimeStampTruncated();

            var modelsFileContent = new StreamReader(_assembly.GetManifestResourceStream("AnkiSharp.AnkiData.models.json")).ReadToEnd();
            var models = modelsFileContent.Replace("{ID_DECK}", id_deck.ToString());

            var json = _flds.ToJSON();
            models = models.Replace("{FLDS}", json);

            var qfmt = _flds[0].ToString();
            var afmt = _flds.ToString();
            models = models.Replace("{QFMT}", qfmt).Replace("{AFMT}", afmt).Replace("\r\n", "");
            
            var deckFileContent = new StreamReader(_assembly.GetManifestResourceStream("AnkiSharp.AnkiData.decks.json")).ReadToEnd();
            var deck = deckFileContent.Replace("{NAME}", _name).Replace("{ID_DECK}", id_deck.ToString()).Replace("\r\n", "");

            var dconfFileContent = new StreamReader(_assembly.GetManifestResourceStream("AnkiSharp.AnkiData.dconf.json")).ReadToEnd();
            var dconf = dconfFileContent.Replace("\r\n", "");

            string insertCol = "INSERT INTO col VALUES(1, " + crt + ", " + timeStamp + ", " + timeStamp + ", 11, 0, 0, 0, '" + conf + "', '" + models + "', '" + deck + "', '" + dconf + "', " + "'{}'" + ");";

            ExecuteSQLiteCommand(insertCol);

            return id_deck;
        }

        private string ConcatFields(AnkiItem item)
        {
            var test = from t in _flds
                       select item[t.Name];

            return String.Join("\x1f", test.ToArray());
        }

        private void CreateNotesAndCards(double id_deck)
        {
            foreach (var ankiItem in _ankiItems)
            {
                var id_note = GetTimeStampTruncated();
                var guid = ((ShortGuid)Guid.NewGuid()).ToString().Substring(0, 10);
                var mid = "1342697561419";
                var mod = GetTimeStampTruncated();
                var flds = ConcatFields(ankiItem);
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
                ExecuteSQLiteCommand(insertNote);

                var id_card = GetTimeStampTruncated();
                string insertCard = "INSERT INTO cards VALUES(" + id_card + ", " + id_note + ", " + id_deck + ", " + "0, " + mod + ", -1, 0, 0, " + id_note + ", 0, 0, 0, 0, 0, 0, 0, 0, '');";

                ExecuteSQLiteCommand(insertCard);
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

                ExecuteSQLiteCommand(column);
                ExecuteSQLiteCommand(notes);
                ExecuteSQLiteCommand(cards);
                ExecuteSQLiteCommand(revLogs);
                ExecuteSQLiteCommand(graves);
                
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

        private void ExecuteSQLiteCommandFromFile(string path)
        {
            string toExecute = File.ReadAllText(path);

            using (SQLiteCommand command = new SQLiteCommand(toExecute, _conn))
            {
                command.ExecuteNonQuery();
            }
        }

        private void ExecuteSQLiteCommand(string toExecute)
        {
            using (SQLiteCommand command = new SQLiteCommand(toExecute, _conn))
            {
                command.ExecuteNonQuery();
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
        #endregion

        #endregion
    }
}
