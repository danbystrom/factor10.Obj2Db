using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace factor10.Obj2Db
{
    public interface ITableFactory
    {
        ITable New(Entity pe, bool hasForeignKey);
        void Save(ITable table);
    }

    public class TableFactory : ITableFactory
    {
        private readonly string _connectionString;

        private ConcurrentDictionary<string, Table> _tables  = new ConcurrentDictionary<string, Table>();
        private readonly HashSet<string> _createdTables = new HashSet<string>();

        public TableFactory(string connectionString)
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
            return new Table(entity, hasForeignKey);
        }

        public void Save(ITable table)
        {
            var tbl = (Table)table;
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
            }
        }

        private void ensureTableCreated(SqlConnection conn, Table table)
        {
            lock (this)
            {
                if (_createdTables.Contains(table.Name))
                    return;
                var prefixedColumns = "[pk] uniqueidentifier not null,";
                if (table.HasForeignKey)
                    prefixedColumns += "[fk] uniqueidentifier not null,";
                using (var cmd = new SqlCommand(SqlStuff.GenerateCreateTable(table, prefixedColumns), conn))
                    cmd.ExecuteNonQuery();
                _createdTables.Add(table.Name);
            }
        }

    }

}
