using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace factor10.Obj2Db
{
    public interface ITable
    {
        string Name { get; }
        void AddRow(Guid pk, Guid fk, object[] columns);
        List<TableRow> Rows { get; }
        List<NameAndType> Fields { get; }
        bool HasForeignKey { get; }
    }

    public sealed class Table : ITable
    {
        public ITableManager TableManager;

        public string Name { get; }
        public bool HasForeignKey { get; }

        public List<NameAndType> Fields { get; }
        public List<TableRow> Rows { get;} = new List<TableRow>();

        private readonly int _flushThreshold;

        public Table(ITableManager tableManager, Entity entity, bool hasForeignKey, int flushThreshold)
        {
            TableManager = tableManager;
            _flushThreshold = flushThreshold;
            Name = entity.ExternalName ?? entity.TypeName;
            HasForeignKey = hasForeignKey;
            Fields = entity.Fields.Select(_ => new NameAndType(_.ExternalName, _.FieldType)).ToList();
            if(Fields.Any(_ => string.IsNullOrEmpty(_.Name)))
                throw new ArgumentException($"Table {Name} contains empty column name");
        }

        public DataTable AsDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("pk", typeof (Guid));
            if (HasForeignKey)
                table.Columns.Add("fk", typeof (Guid));
            foreach (var field in Fields)
                table.Columns.Add(field.Name, LinkedFieldInfo.StripNullable(field.Type));
            foreach (var itm in Rows)
            {
                var row = table.NewRow();
                var idx = 0;
                row[idx++] = itm.PrimaryKey;
                if (HasForeignKey)
                    row[idx++] = itm.ParentRow;
                foreach (var obj in itm.Columns)
                    row[idx++] = obj ?? DBNull.Value;
                table.Rows.Add(row);
            }
            return table;
        }

        public void AddRow(Guid pk, Guid fk, object[] columns)
        {
            if (columns.Length != Fields.Count)
                throw new ArgumentException();
            if (Rows.Count >= _flushThreshold)
            {
                TableManager.Save(this);
                Rows.Clear();
            }
            var tableRow = new TableRow(pk, fk) {Columns = columns};
            Rows.Add(tableRow);
        }

    }

    public class TableRow
    {
        public readonly Guid PrimaryKey;
        public readonly Guid ParentRow;
        public object[] Columns;

        public TableRow(Guid pk, Guid parentRow)
        {
            PrimaryKey = pk;
            ParentRow = parentRow;
        }

    }

}
