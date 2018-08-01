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
    }
}
