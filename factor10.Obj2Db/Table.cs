using System;
using System.Collections.Generic;
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

    public class InMemoryTable : ITable
    {
        public string Name { get; }

        public List<NameAndType> Fields { get; }
        public int PrimaryKeyIndex { get; }
        public bool IsTopTable { get; }
        public bool IsLeafTable { get; }
        public int SavedRowCount => Rows.Count;
        public List<TableRow> Rows { get; }

        public void AddRow(object pk, object fk, object[] columns)
        {
            Rows.Add(new TableRow(pk, fk) {Columns = columns.Select(_ => _ ?? DBNull.Value).ToArray()});
        }

        public InMemoryTable(ITable table)
        {
            Name = table.Name;
            Fields = table.Fields;
            PrimaryKeyIndex = table.PrimaryKeyIndex;
            IsTopTable = table.IsTopTable;
            IsLeafTable = table.IsLeafTable;
            Rows = table.Rows;
        }

        public InMemoryTable(
            Entity entity,
            bool isTopTable,
            bool isLeafTable,
            int primaryKeyIndex)
        {
            Name = entity.ExternalName;
            IsTopTable = isTopTable;
            IsLeafTable = isLeafTable;
            PrimaryKeyIndex = primaryKeyIndex;
            Fields = entity.Fields.Where(_ => !_.NoSave).Select(_ => new NameAndType(_.ExternalName, _.FieldType)).ToList();
            if (Fields.Any(_ => string.IsNullOrEmpty(_.Name)))
                throw new ArgumentException($"Table '{Name}' contains empty column name");
            var dupCheck = Fields.ToLookup(_ => _.Name.ToUpper(), _ => _).Where(_ => _.Count() != 1).Select(_ => _.Key).ToList();
            if (dupCheck.Any())
                throw new ArgumentException($"Table '{Name}' contains duplicate column names '{string.Join("','", dupCheck)}'");
            Rows = new List<TableRow>();
        }

    }

}