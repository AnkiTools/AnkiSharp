using System;
using System.Data.SQLite;

namespace AnkiSharp.Helpers
{
    internal class SQLiteHelper
    {
    
        internal static void ExecuteSQLiteCommand(SQLiteConnection conn, string toExecute)
        {
            try
            {
                using (SQLiteCommand command = new SQLiteCommand(toExecute, conn))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Can't execute query : " + toExecute);
            }
        }

        internal static SQLiteDataReader ExecuteSQLiteCommandRead(SQLiteConnection conn, string toExecute)
        {
            try
            {
                using (SQLiteCommand command = new SQLiteCommand(toExecute, conn))
                {
                    return command.ExecuteReader();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Can't execute query : " + toExecute);
            }
        }

        internal static string CreateStringFormat(int from, int to)
        {
            string result = "";

            for (int i = from; i < to; ++i)
            {
                result += "{" + i.ToString() + "}";

                if (i + 1 < to)
                    result += ", ";
            }

            return result;
        }
    }
}
