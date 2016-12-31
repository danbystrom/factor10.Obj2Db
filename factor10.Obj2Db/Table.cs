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
        int PrimaryKeyIndex { get; }
        int ForeignKeyIndex { get; }
        bool IsTopTable { get; }
        bool IsLeafTable { get; }
        int SavedRowCount { get; }
    }

    public sealed class Table : ITable
    {
        public ITableManager TableManager;

        public string Name { get; }
        public bool IsTopTable { get; }
        public bool IsLeafTable { get; }
        public int ForeignKeyIndex { get; }
        public int PrimaryKeyIndex { get; }
        public int SavedRowCount { get; private set; }

        public List<NameAndType> Fields { get; }
        public List<TableRow> Rows { get;} = new List<TableRow>();

        private readonly int _flushThreshold;

        public Table(
            ITableManager tableManager, 
            Entity entity,
            bool isTopTable,
            bool isLeafTable,
            int primaryKeyIndex,
            int foreignyKeyIndex,
            int flushThreshold)
        {
            TableManager = tableManager;
            _flushThreshold = flushThreshold;
            Name = entity.ExternalName;
            PrimaryKeyIndex = primaryKeyIndex;
            ForeignKeyIndex = foreignyKeyIndex;
            Fields = entity.Fields.Where(_ => !_.NoSave).Select(_ => new NameAndType(_.ExternalName, _.FieldType)).ToList();
            if(Fields.Any(_ => string.IsNullOrEmpty(_.Name)))
                throw new ArgumentException($"Table {Name} contains empty column name");
        }

        public DataTable ExtractDataTable()
        {
            var table = new DataTable();
            if(PrimaryKeyIndex<0)
                table.Columns.Add("_id_", typeof (Guid));
            if (ForeignKeyIndex<0)
                table.Columns.Add("_fk_", typeof (Guid));
            foreach (var field in Fields)
                table.Columns.Add(field.Name, LinkedFieldInfo.StripNullable(field.Type));
            foreach (var itm in Rows)
            {
                var row = table.NewRow();
                var idx = 0;
                if (PrimaryKeyIndex < 0)
                    row[idx++] = itm.PrimaryKey;
                if (ForeignKeyIndex<0)
                    row[idx++] = itm.ParentRow;
                for (var col = 0; col < Fields.Count; col++)
                    row[idx++] = itm.Columns[col] ?? DBNull.Value;
                table.Rows.Add(row);
            }
            SavedRowCount += Rows.Count;
            Rows.Clear();
            return table;
        }

        public void AddRow(Guid pk, Guid fk, object[] columns)
        {
            if (columns.Length < Fields.Count)  // notsaved columns are passed here and will/should be truncated later
                throw new ArgumentException();
            if (Rows.Count >= _flushThreshold)
                TableManager.Save(this);
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
