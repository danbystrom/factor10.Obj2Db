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
        void Begin();
        void End();
        void Save(ITable table);
        List<ITable> GetWithAllData();
        Dictionary<string, int> GetExportedSummary();
    }

    public sealed class SqlTableManager : ITableManager
    {
        private readonly string _connectionString;
        private readonly string _logTableName;
        private bool _hasVerifiedLogTableExistence;

        private readonly ConcurrentBag<ITable> _tables = new ConcurrentBag<ITable>();

        public int FlushThreshold = 5000;

        public Guid LogSessionId = Guid.NewGuid();
        public int LogSequenceId = 0;

        public SqlTableManager(string connectionString, string logTableName = null)
        {
            _connectionString = connectionString;
            _logTableName = logTableName;
        }

        public void WriteLog(string text)
        {
            using (var conn = getOpenConnection())
                writeLog(conn, text);
        }

        private void writeLog(SqlConnection conn, string text)
        {
            if (_logTableName == null)
                return;
            ensureLogTableCreated(conn);
            using (var cmd = new SqlCommand($"INSERT INTO {_logTableName} ([sessionid],[seqid],[when],[text]) VALUES (@sessionid,@seqid,@when,@text)", conn))
            {
                cmd.Parameters.AddWithValue("@sessionid", LogSessionId);
                cmd.Parameters.AddWithValue("@seqid", LogSequenceId++);
                cmd.Parameters.AddWithValue("@when", DateTime.Now);
                cmd.Parameters.AddWithValue("@text", text);
                cmd.ExecuteNonQuery();
            }
        }

        private void ensureLogTableCreated(SqlConnection conn)
        {
            if (_hasVerifiedLogTableExistence)
                return;
            _hasVerifiedLogTableExistence = true;
            if (GetExistingTableNames(conn).Contains(_logTableName.ToUpper()))
                return;
            var cmd = $"CREATE TABLE [{_logTableName}] ([sessionid] uniqueidentifier not null, [seqid] int not null, [when] datetime not null, [text] nvarchar(max))";
            executeCommand(conn, new[] {cmd}, false);
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

        private static string cutTableName(string s, string tmp)
        {
            s = s + tmp;
            return s.Substring(s.Length - Math.Min(s.Length, 128));
        }

        private static string useTableName(string s)
        {
            return cutTableName(s, "_tmp");
        }

        private static string bckTableName(string s)
        {
            return cutTableName(s, "_bck");
        }

        public void Begin()
        {
            var tables = _tables.ToLookup(_ => _.Name).ToDictionary(_ => _.Key, _ => (SqlTable)_.First());
            using (var conn = getOpenConnection())
            {
                var allExisting = GetExistingTableNames(conn);
                var useTables = tables.Keys.Select(_ => useTableName(_).ToUpper());
                var clashingUse = useTables.Where(_ => allExisting.Contains(_)).ToList();
                executeCommand(conn, clashingUse.Select(_ => $"DROP TABLE {_}"));
                executeCommand(conn, tables.Select(_ => _.Value.GenerateCreateTable(useTableName(_.Key)))); 
            }
        }

        public void End()
        {
            //flush
            foreach (var table in _tables)
                Save(table);

            var tables = _tables.ToLookup(_ => _.Name).ToDictionary(_ => _.Key, _ => _.Cast<SqlTable>().ToList());
            using (var conn = getOpenConnection())
            {
                // drop _bck-tables, rename existing tables to _back and then rename _tmp-tales to the real names
                var allExisting = GetExistingTableNames(conn);
                var bckTables = tables.Keys.Select(_ => bckTableName(_).ToUpper());
                var clashingBck = bckTables.Where(_ => allExisting.Contains(_)).ToList();
                executeCommand(conn, clashingBck.Select(_ => $"DROP TABLE {_}"));
                var clashingReal = tables.Keys.Where(_ => allExisting.Contains(_.ToUpper())).ToList();
                executeCommand(conn, clashingReal.Select(_ => $"EXEC sp_rename '{_}', '{bckTableName(_)}'"));
                executeCommand(conn, tables.Keys.Select(_ => $"EXEC sp_rename '{useTableName(_)}', '{_}'"));

                foreach (var sqltables in tables.Values)
                {
                    var table = sqltables.First();
                    if (!table.IsTopTable)
                        using (var cmd = new SqlCommand($"CREATE INDEX {table.Name}_fk ON {table.Name}({table.ForeignKeyName})", conn))
                            cmd.ExecuteNonQuery();

                    var expectedRowCount = sqltables.Sum(_ => _.SavedRowCount);
                    using (var cmd = new SqlCommand($"SELECT COUNT(*) FROM {table.Name}", conn))
                    {
                        var actualRowCount = (int) cmd.ExecuteScalar();
                        if (actualRowCount != expectedRowCount)
                            throw new Exception($"Found {actualRowCount} rows in table {table.Name} but expected {expectedRowCount}");
                    }
                }

                writeLog(conn, "Verified table row counts: " + string.Join(";",
                                   tables.Values.Select(t => $"{t.First().Name}:{t.Sum(_ => _.SavedRowCount)}")));
            }
        }

        private void executeCommand(SqlConnection conn, IEnumerable<string> sqls, bool log=true)
        {
            var list = sqls.ToList();
            if (!list.Any())
                return;
            var cmdText = string.Join(";", list);
            if(log)
                writeLog(conn, cmdText);
            using (var cmd = new SqlCommand(cmdText, conn))
                cmd.ExecuteNonQuery();
        }

        public HashSet<string> GetExistingTableNames(SqlConnection conn, bool toUpper = true)
        {
            var existing = new HashSet<string>();
            using (var cmd = new SqlCommand("SELECT name FROM sys.Tables WHERE type='U'", conn))
            using (var reader = cmd.ExecuteReader())
                while (reader.Read())
                    existing.Add(toUpper ? reader.GetString(0).ToUpper(): reader.GetString(0));
            return existing;
        }

        public void Save(ITable table)
        {
            var tbl = (SqlTable) table;
            using (var conn = getOpenConnection())
            {
                var bulkCopy = new SqlBulkCopy(
                    conn,
                    SqlBulkCopyOptions.TableLock |
                    SqlBulkCopyOptions.FireTriggers |
                    SqlBulkCopyOptions.UseInternalTransaction,
                    null) {DestinationTableName = useTableName(table.Name)};
                tbl.WithDataTable(bulkCopy.WriteToServer);
            }
        }

        public List<ITable> GetWithAllData()
        {
            throw new NotImplementedException();
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

        public void Begin()
        {
        }

        public void End()
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
