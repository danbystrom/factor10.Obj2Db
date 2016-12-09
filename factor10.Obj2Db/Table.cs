using System;
using System.Collections.Generic;
using System.Text;

namespace factor10.Obj2Db
{
    public class Table
    {
        public readonly string[] FieldNames;
        public readonly List<TableRow> Rows;
        public readonly Dictionary<Type, Table> SubTables;
    }

    public class TableRow
    {
        public readonly Guid PrimaryKey;
        public readonly Guid ParentRow;
        public readonly List<object[]> Rows;

        public TableRow(Guid parentRow)
        {
            PrimaryKey = Guid.NewGuid();
            ParentRow = parentRow;
            Rows = new List<object[]>();
        }

    }

}
