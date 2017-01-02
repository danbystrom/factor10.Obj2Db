using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace factor10.Obj2Db
{
    public class SqlHelpers
    {

        public static Dictionary<string, string> ColumnTypes = new Dictionary<string, string>
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
            {"System.Guid", "uniqueidentifier"},
            {"System.Byte[]", "varbinary(max)"}
        };

        public static string Field2Sql(NameAndType field, bool returnNullWhenInvalidType = false)
        {
            var type = field.Type;
            var notnull = type != typeof(string) && type != typeof(byte[]);
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = type.GenericTypeArguments[0];
                notnull = false;
            }
            if (type.IsEnum)
                type = typeof(int);
            string def;
            if (!ColumnTypes.TryGetValue(type.FullName, out def))
                if (returnNullWhenInvalidType)
                    return null;
                else
                    throw new Exception($"Unhandled column type '{type}'");
            return $"[{field.Name}] {def}" + (notnull ? " not null" : "");
        }

        public static string GenerateCreateTable(ITable table, string prefixedColumns)
        {
            return $"CREATE TABLE [{table.Name}] ({prefixedColumns}{string.Join(",", table.Fields.Select(_ => Field2Sql(_)))})";
        }

    }

}