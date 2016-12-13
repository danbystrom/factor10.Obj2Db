using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace factor10.Obj2Db
{
    public interface ITable
    {
        string Name { get; }
        TableRow AddRow(Guid parentRowId, object[] columns);
    }

    public class Table : ITable
    {
        public string Name { get; }
        public readonly bool HasForeignKey;

        public readonly Guid Id = Guid.NewGuid();

        public readonly List<Tuple<string, Type>> Fields;
        public readonly List<TableRow> Rows = new List<TableRow>();

        public Table(Entity entity, bool hasForeignKey)
        {
            Name = entity.Name ?? entity.TypeName;
            HasForeignKey = hasForeignKey;
            Fields = entity.Fields.Select(_ => Tuple.Create(_.ExternalName, _.FieldInfo.FieldType)).ToList();
        }

        public DataTable AsDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("pk", typeof (Guid));
            if (HasForeignKey)
                table.Columns.Add("fk", typeof (Guid));
            foreach (var field in Fields)
                table.Columns.Add(field.Item1, field.Item2);
            foreach (var itm in Rows)
            {
                var row = table.NewRow();
                var idx = 0;
                row[idx++] = itm.PrimaryKey;
                if (HasForeignKey)
                    row[idx++] = itm.ParentRow;
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
