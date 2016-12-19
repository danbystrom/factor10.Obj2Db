using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace factor10.Obj2Db
{
    public class SqlStuff
    {
        private const string ConnectionStringBase = @"Data Source=localhost\SQLEXPRESS;Initial Catalog={0};Integrated Security=SSPI;";

        public static string ConnectionString(string dbName)
        {
            return string.Format(ConnectionStringBase, dbName);
        }

        public static void WithNewDb(string dbName, Action<SqlConnection> work)
        {
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

        public static object[] SimpleQuery(SqlConnection conn, string query)
        {
            using (var cmd = new SqlCommand(query, conn))
            using (var reader = cmd.ExecuteReader())
            {
                reader.Read();
                var objs = new object[reader.FieldCount];
                reader.GetValues(objs);
                return objs;
            }
        }

        public static T SimpleQuery<T>(SqlConnection conn, string query)
        {
            return (T) SimpleQuery(conn, query)[0];
        }

        public static string Field2Sql(NameAndType field)
        {
            var type = field.Type;
            var dic = new Dictionary<string, string>
            {
                {"System.Int32", "integer"},
                {"System.Int64", "bigint"},
                {"System.Int16", "smallint"},
                {"System.Decimal", "float"},
                {"System.DateTime", "datetime"},
                {"System.Double", "float"},
                {"System.Single", "float"},
                {"System.String", "nvarchar(max)"},
                {"System.Boolean", "bit"},
                {"System.Guid", "uniqueidentifier"}
            };
            var notnull = type != typeof(string);
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = type.GenericTypeArguments[0];
                notnull = false;
            }
            if (type.IsEnum)
                type = typeof(int);
            string def;
            if (!dic.TryGetValue(type.ToString(), out def))
                throw new Exception($"Unhandled column type '{type}'");
            return $"[{field.Name}] {def}" + (notnull ? " not null" : "");
        }

        public static string GenerateCreateTable(ITable table, string prefixedColumns)
        {
            return $"CREATE TABLE [{table.Name}] ({prefixedColumns}{string.Join(",", table.Fields.Select(Field2Sql))})";
        }

    }

}