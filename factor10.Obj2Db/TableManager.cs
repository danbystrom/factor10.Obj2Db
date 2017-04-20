using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace factor10.Obj2Db
{
    public interface ITableManager
    {
        ITable New(EntityClass entity, bool isTopTable, bool isLeafTable, int primaryKeyIndex);
        void Save(ITable table);
        void Flush();
        List<ITable> GetWithAllData();
        Dictionary<string, int> GetExportedSummary();
    }

    public sealed class SqlTableManager : ITableManager
    {
        private readonly string _connectionString;

        private readonly ConcurrentBag<ITable> _tables = new ConcurrentBag<ITable>();
        private readonly HashSet<string> _createdTables = new HashSet<string>();

        public int FlushThreshold = 5000;

        public SqlTableManager(string connectionString)
        {
            _connectionString = connectionString;
        }

        private SqlConnection getOpenConnection()
        {
            var conn = new SqlConnection(_connectionString);
            conn.Open();
            return conn;
        }

        public ITable New(EntityClass entity, bool isTopTable, bool isLeafTable, int primaryKeyIndex)
        {
            var table = new SqlTable(this, entity, isTopTable, isLeafTable, primaryKeyIndex, FlushThreshold);
            _tables.Add(table);
            return table;
        }

        public void Save(ITable table)
        {
            var tbl = (SqlTable) table;
            using (var conn = getOpenConnection())
            {
                ensureTableCreated(conn, tbl);
                var bulkCopy = new SqlBulkCopy(
                    conn,
                    SqlBulkCopyOptions.TableLock |
                    SqlBulkCopyOptions.FireTriggers |
                    SqlBulkCopyOptions.UseInternalTransaction,
                    null) {DestinationTableName = table.Name};
                tbl.WithDataTable(bulkCopy.WriteToServer);
            }
        }

        public void Flush()
        {
            var tableWithFks = new HashSet<string>();
            foreach (var table in _tables)
                Save(table);
            using (var conn = getOpenConnection())
                foreach (var table in _tables)
                    if (!table.IsTopTable && !tableWithFks.Contains(table.Name))
                    {
                        tableWithFks.Add(table.Name);
                        using (var cmd = new SqlCommand($"CREATE INDEX {table.Name}_fk ON {table.Name}(_fk_)", conn))
                            cmd.ExecuteNonQuery();
                    }
        }

        public List<ITable> GetWithAllData()
        {
            throw new NotImplementedException();
        }

        private void ensureTableCreated(SqlConnection conn, SqlTable table)
        {
            lock (this)
            {
                if (_createdTables.Contains(table.Name))
                    return;
                _createdTables.Add(table.Name);
                var prefixedColumns = "";
                if (table.AutomaticPrimaryKey)
                    prefixedColumns = "[_id_] uniqueidentifier not null,";
                if (!table.IsTopTable)
                    prefixedColumns += "[_fk_] uniqueidentifier not null,";
                var sql = table.GenerateCreateTable(prefixedColumns);
                Console.WriteLine($"Will create '{table.Name}': {sql}");
                using (var cmd = new SqlCommand(sql, conn))
                    cmd.ExecuteNonQuery();
            }
        }

        public Dictionary<string, int> GetExportedSummary()
        {
            var lookup = _tables.ToLookup(_ => _.Name, _ => _.SavedRowCount);
            return lookup.ToDictionary(_ => _.Key, _ => _.Sum());
        }

    }

    public class InMemoryTableManager : ITableManager
    {
        public readonly List<ITable> Tables = new List<ITable>();

        public ITable New(EntityClass entity, bool isTopTable, bool isLeafTable, int primaryKeyIndex)
        {
            lock (this)
            {
                var table = new InMemoryTable(entity, isTopTable, isLeafTable, primaryKeyIndex);
                Tables.Add(table);
                return table;
            }
        }

        public void Save(ITable table)
        {
        }

        public void Flush()
        {
        }

        public List<ITable> GetWithAllData()
        {
            var joined = Tables.ToLookup(_ => _.Name, _ => _);
            var result = new List<ITable>();
            foreach (var z in joined)
            {
                var tables = z.ToList();
                var t = new InMemoryTable(tables[0]);
                for (var i = 1; i < tables.Count; i++)
                    foreach (var tr in tables[i].Rows)
                        t.Rows.Add(tr);
                result.Add(t);
            }
            foreach (var t in result)
                foreach (var row in t.Rows)
                    Array.Resize(ref row.Columns, t.Fields.Count);
            return result;
        }

        public Dictionary<string, int> GetExportedSummary()
        {
            var lookup = Tables.ToLookup(_ => _.Name, _ => _.Rows.Count);
            return lookup.ToDictionary(_ => _.Key, _ => _.Sum());
        }

    }

}
