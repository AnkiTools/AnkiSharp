using System.Data.SQLite;

namespace AnkiSharp.Helpers
{
    internal class SQLiteHelper
    {
        internal static void ExecuteSQLiteCommand(SQLiteConnection conn, string toExecute)
        {
            using (SQLiteCommand command = new SQLiteCommand(toExecute, conn))
            {
                command.ExecuteNonQuery();
            }
        }
    }
}
