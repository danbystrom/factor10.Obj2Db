using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace factor10.Obj2Db
{
    public sealed class SqlTable : ITable
    {
        public ITableManager TableManager;

        public string Name { get; }
        public bool IsTopTable { get; }
        public bool IsLeafTable { get; }
        public int PrimaryKeyIndex { get; }
        public int SavedRowCount { get; private set; }

        public List<NameAndType> Fields { get; }

        public bool AutomaticPrimaryKey => !IsLeafTable && PrimaryKeyIndex < 0;

        private readonly int _flushThreshold;

        private readonly DataTable _dataTable;

        public SqlTable(
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

            _dataTable = new DataTable();
            if (AutomaticPrimaryKey)
                _dataTable.Columns.Add("_id_", typeof(Guid));
            if (!IsTopTable)
                _dataTable.Columns.Add("_fk_", typeof(Guid));
            foreach (var field in Fields)
                _dataTable.Columns.Add(field.Name, LinkedFieldInfo.StripNullable(field.Type));
        }

        public void WithDataTable(Action<DataTable> action)
        {
            action(_dataTable);
            SavedRowCount += _dataTable.Rows.Count;
            _dataTable.Rows.Clear();
        }

        public void AddRow(object pk, object fk, object[] columns)
        {
            if (columns.Length < Fields.Count)  // notsaved columns are passed here and will/should be truncated later
                throw new ArgumentException();

            var row = _dataTable.NewRow();
            var idx = 0;
            if (AutomaticPrimaryKey)
                row[idx++] = pk;
            if (!IsTopTable)
                row[idx++] = fk;
            for (var col = 0; col < Fields.Count; col++)
                row[idx++] = columns[col] ?? DBNull.Value;
            _dataTable.Rows.Add(row);

            if (_dataTable.Rows.Count >= _flushThreshold)
                TableManager.Save(this);
        }

        public List<TableRow> Rows
        {
            get
            {
                var rows = new List<TableRow>();
                foreach (DataRow datarow in _dataTable.Rows)
                {
                    var idx = 0;
                    object pk = null, fk = null;
                    if (AutomaticPrimaryKey)
                        pk = datarow[idx++];
                    if (!IsTopTable)
                        fk = datarow[idx++];
                    var row = new TableRow(pk, fk) {Columns = new object[Fields.Count]};
                    for (var col = 0; col < Fields.Count; col++)
                        row.Columns[col] = datarow[idx++];
                    rows.Add(row);
                }
                return rows;
            }
        }
    }

}
