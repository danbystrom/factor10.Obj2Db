using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace factor10.Obj2Db
{
    public interface ITable
    {
        string Name { get; }
        void AddRow(object pk, object fk, object[] columns);
        List<TableRow> Rows { get; }
        List<NameAndType> Fields { get; }
        int PrimaryKeyIndex { get; }
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
        public int PrimaryKeyIndex { get; }
        public int SavedRowCount { get; private set; }

        public List<NameAndType> Fields { get; }
        public List<TableRow> Rows { get;} = new List<TableRow>();

        public bool AutomaticPrimaryKey => !IsLeafTable && PrimaryKeyIndex < 0;

        private readonly int _flushThreshold;

        public Table(
            ITableManager tableManager, 
            Entity entity,
            bool isTopTable,
            bool isLeafTable,
            int primaryKeyIndex,
            int flushThreshold)
        {
            TableManager = tableManager;
            _flushThreshold = flushThreshold;
            Name = entity.ExternalName;
            IsTopTable = isTopTable;
            IsLeafTable = isLeafTable;
            PrimaryKeyIndex = primaryKeyIndex;
            Fields = entity.Fields.Where(_ => !_.NoSave).Select(_ => new NameAndType(_.ExternalName, _.FieldType)).ToList();
            if(Fields.Any(_ => string.IsNullOrEmpty(_.Name)))
                throw new ArgumentException($"Table {Name} contains empty column name");
        }

        public DataTable ExtractDataTable()
        {
            var table = new DataTable();
            if(AutomaticPrimaryKey)
                table.Columns.Add("_id_", typeof (Guid));
            if (!IsTopTable)
                table.Columns.Add("_fk_", typeof (Guid));
            foreach (var field in Fields)
                table.Columns.Add(field.Name, LinkedFieldInfo.StripNullable(field.Type));
            foreach (var itm in Rows)
            {
                var row = table.NewRow();
                var idx = 0;
                if (AutomaticPrimaryKey)
                    row[idx++] = itm.PrimaryKey;
                if (!IsTopTable)
                    row[idx++] = itm.ParentRow;
                for (var col = 0; col < Fields.Count; col++)
                    row[idx++] = itm.Columns[col] ?? DBNull.Value;
                table.Rows.Add(row);
            }
            SavedRowCount += Rows.Count;
            Rows.Clear();
            return table;
        }

        public void AddRow(object pk, object fk, object[] columns)
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
        public readonly object PrimaryKey;
        public readonly object ParentRow;
        public object[] Columns;

        public TableRow(object pk, object parentRow)
        {
            PrimaryKey = pk;
            ParentRow = parentRow;
        }

    }

}
