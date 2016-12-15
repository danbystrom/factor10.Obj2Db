﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

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

        private readonly ConcurrentBag<ITable> _tables = new ConcurrentBag<ITable>();
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
        public readonly List<Table> Tables = new List<Table>();
         
        public ITable New(Entity entity, bool hasForeignKey)
        {
            lock (this)
            {
                var table = new Table(this, entity, hasForeignKey, int.MaxValue);
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

        public List<ITable> GetMergedTables()
        {
            var joined = Tables.ToLookup(_ => _.Name, _ => _);
            var result = new List<ITable>();
            foreach (var z in joined)
            {
                var tables = z.ToList();
                for (var i = 1; i < tables.Count; i++)
                    foreach (var tr in tables[i].Rows)
                        tables[0].Rows.Add(tr);
                result.Add(tables[0]);
            }
            return result;
        }

    }

}
