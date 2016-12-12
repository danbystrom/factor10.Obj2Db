using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace factor10.Obj2Db
{
    public class Table
    {
        public readonly string Name;
        public readonly bool IsTop;

        public readonly List<Tuple<string, Type>> Fields;
        public readonly List<TableRow> Rows = new List<TableRow>();

        public Table(ProcessedEntity entity, bool isTop)
        {
            Name = entity.Name;
            IsTop = isTop;
            Fields = entity.Fields.Select(_ => Tuple.Create(_.ExternalName, _.FieldInfo.FieldType)).ToList();
        }

        public DataTable AsDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("id", typeof (Guid));
            if (!IsTop)
                table.Columns.Add("parent", typeof (Guid));
            table.Columns.Add("id", typeof (Guid));
            foreach (var field in Fields)
                table.Columns.Add(field.Item1, field.Item2);
            foreach (var itm in Rows)
            {
                var row = table.NewRow();
                var idx = 0;
                row[idx++] = itm.PrimaryKey;
                if (!IsTop)
                    row[idx++] = itm.PrimaryKey;
                foreach (var obj in itm.Columns)
                    row[idx++] = obj;
                table.Rows.Add(row);
            }
            return table;
        }

        public TableRow AddRow(Guid parentRowId, object[] columns)
        {
            if (columns.Length != Fields.Count)
                throw new ArgumentException();
            var tableRow = new TableRow(parentRowId) {Columns = columns};
            Rows.Add(tableRow);
            return tableRow;
        }

    }

    public class TableRow
    {
        public readonly Guid PrimaryKey;
        public readonly Guid ParentRow;
        public object[] Columns;

        public TableRow(Guid parentRow)
        {
            PrimaryKey = Guid.NewGuid();
            ParentRow = parentRow;
        }

    }

}
