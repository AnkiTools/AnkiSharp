
using AnkiSharp.Models;
using System.Collections.Generic;
using System.Data.SQLite;

namespace AnkiSharp.Helpers
{
    internal class Mapper
    {
        private static Mapper instance = null;

        private Mapper()
        {
        }

        public static Mapper Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Mapper();
                }
                return instance;
            }
        }
        
        public static List<AnkiSharpDynamic> MapSQLiteReader(SQLiteConnection conn, string toExecute)
        {
            List<AnkiSharpDynamic> result = new List<AnkiSharpDynamic>();
            SQLiteDataReader reader = SQLiteHelper.ExecuteSQLiteCommandRead(conn, toExecute);

            while (reader.Read())
            {
                AnkiSharpDynamic ankiSharpDynamic = new AnkiSharpDynamic();

                for (int i = 0; i < reader.FieldCount; ++i)
                {
                    ankiSharpDynamic[reader.GetName(i)] = reader.GetValue(i);
                }

                result.Add(ankiSharpDynamic);
            }

            reader.Close();
            return result;
        }
    }
}
