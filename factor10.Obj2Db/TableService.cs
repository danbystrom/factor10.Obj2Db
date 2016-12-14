using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace factor10.Obj2Db
{
    public interface ITableService
    {
        ITable New(Entity entity, bool hasForeignKey);
        void Save(ITable table);
        void Flush();
    }

    public class SqlTableService : ITableService
    {
        private readonly string _connectionString;

        private ConcurrentBag<ITable> _tables = new ConcurrentBag<ITable>();
        private readonly HashSet<string> _createdTables = new HashSet<string>();

        public int FlushThreshold = 5000;

        public SqlTableService(string connectionString)
        {
            _connectionString = connectionString;
        }

        private SqlConnection getOpenConnection()
        {
            var conn = new SqlConnection(_connectionString);
            conn.Open();
            return conn;
        }

        public ITable New(Entity entity, bool hasForeignKey)
        {
            var table = new Table(this, entity, hasForeignKey, FlushThreshold);
            _tables.Add(table);
            return table;
        }

        public void Save(ITable table)
        {
            var tbl = (Table) table;
            using (var conn = getOpenConnection())
            {
                ensureTableCreated(conn, tbl);
                var bulkCopy = new SqlBulkCopy(
                    conn,
                    SqlBulkCopyOptions.TableLock |
                    SqlBulkCopyOptions.FireTriggers |
                    SqlBulkCopyOptions.UseInternalTransaction,
                    null) {DestinationTableName = table.Name};
                bulkCopy.WriteToServer(tbl.AsDataTable());
                tbl.Rows.Clear();
            }
        }

        public void Flush()
        {
            var tableWithFks = new HashSet<string>();
            foreach (var table in _tables)
                Save(table);
            using (var conn = getOpenConnection())
                foreach (var table in _tables)
                    if (table.HasForeignKey && !tableWithFks.Contains(table.Name))
                    {
                        tableWithFks.Add(table.Name);
                        using (var cmd = new SqlCommand($"CREATE INDEX {table.Name}_fk ON {table.Name}(fk)", conn))
                            cmd.ExecuteNonQuery();
                    }
        }

        private void ensureTableCreated(SqlConnection conn, Table table)
        {
            lock (this)
            {
                if (_createdTables.Contains(table.Name))
                    return;
                Console.WriteLine($"Will create '{table.Name}'");
                _createdTables.Add(table.Name);
                var prefixedColumns = "[pk] uniqueidentifier not null,";
                if (table.HasForeignKey)
                    prefixedColumns += "[fk] uniqueidentifier not null,";
                using (var cmd = new SqlCommand(SqlStuff.GenerateCreateTable(table, prefixedColumns), conn))
                    cmd.ExecuteNonQuery();
            }
        }

    }

    public class InMemoryTableService : ITableService
    {
        public ITable New(Entity entity, bool hasForeignKey)
        {
            return new Table(this, entity, hasForeignKey, int.MaxValue);
        }

        public void Save(ITable table)
        {
        }

        public void Flush()
        {
        }

    }

}
