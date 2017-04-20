using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;

namespace factor10.Obj2Db.Tests.Database
{
    public class QueryResult
    {
        public NameAndType[] NameAndTypes;
        public List<object[]> Rows;
    }

    public class SqlTestHelpers
    {
        private static readonly string _connectionString;

        static SqlTestHelpers()
        {
            _connectionString = ConfigurationManager.AppSettings["connectionstringbase"];
        }

        public static string ConnectionString(string dbName)
        {
            return !string.IsNullOrEmpty(_connectionString)
                ? string.Format(_connectionString, dbName ?? "")
                : null;
        }

        public static void WithNewDb(string dbName, Action<SqlConnection> work)
        {
            var connectionString = ConnectionString(dbName);
            if (connectionString == null)
                return;
            CreateAndDropDatabase(dbName, true);
            using (var conn = new SqlConnection(ConnectionString(dbName)))
                try
                {
                    conn.Open();
                    work(conn);
                }
                finally
                {
                    conn.Close();
                    //CreateAndDropDatabase(dbName, false);
                }
        }

        public static void CreateAndDropDatabase(string dbName, bool create)
        {
            var drop = $"ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{dbName}];";

            using (var conn = new SqlConnection(ConnectionString("master")))
            {
                conn.Open();
                try
                {
                    using (var cmd = new SqlCommand(drop, conn))
                        cmd.ExecuteNonQuery();
                }
                catch
                {
                }
                if (create)
                    using (var cmd = new SqlCommand($"CREATE DATABASE [{dbName}]", conn))
                        cmd.ExecuteNonQuery();
            }
        }

        public static QueryResult SimpleQuery(SqlConnection conn, string query)
        {
            using (var cmd = new SqlCommand(query, conn))
            using (var reader = cmd.ExecuteReader())
            {
                var result = new QueryResult
                {
                    NameAndTypes = Enumerable.Range(0, reader.FieldCount).Select(_ => new NameAndType(
                        reader.GetName(_), reader.GetFieldType(_))).ToArray(),
                    Rows = new List<object[]>()
                };
                while (reader.Read())
                {
                    var objs = new object[reader.FieldCount];
                    reader.GetValues(objs);
                    result.Rows.Add(objs);
                }
                return result;
            }
        }

        public static T SimpleQuery<T>(SqlConnection conn, string query)
        {
            return (T) SimpleQuery(conn, query).Rows[0][0];
        }

    }

}
