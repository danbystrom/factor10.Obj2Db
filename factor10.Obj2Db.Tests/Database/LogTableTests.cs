using System;
using System.Collections.Generic;
using System.Linq;
using factor10.Obj2Db.Tests.TestData;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests.Database
{
    [TestFixture]
    public class WhenWritingSeveralSessions : SchoolBaseTests
    {
        private class logEntry
        {
            public readonly Guid SessionId;
            public readonly int SeqId;
            public readonly DateTime When;
            public readonly string Text;

            public logEntry(object[] row)
            {
                SessionId = (Guid)row[0];
                SeqId = (int)row[1];
                When = (DateTime)row[2];
                Text = (string)row[3];
            }
        }

        private List<logEntry> _logRows;
        private List<logEntry> _log1;
        private List<logEntry> _log2;
        private List<logEntry> _log3;

        [OneTimeSetUp]
        public void SetUp()
        {
            if (SqlTestHelpers.ConnectionString(null) == null)
                return;
            SqlTestHelpers.WithNewDb("LogTest", conn =>
            {
                var tableFactory = new SqlTableManager(SqlTestHelpers.ConnectionString("LogTest"), "log");
                var export = new DataExtract<School>(Spec, tableFactory);
                export.Run(new[] {School});

                tableFactory = new SqlTableManager(SqlTestHelpers.ConnectionString("LogTest"), "log");
                tableFactory.WriteLog("Explicit logging from the outside A");
                export = new DataExtract<School>(Spec, tableFactory);
                export.Run(new[] { School });
                tableFactory.WriteLog("Explicit logging from the outside B");

                tableFactory = new SqlTableManager(SqlTestHelpers.ConnectionString("LogTest"), "log");
                tableFactory.WriteLog("Explicit logging from the outside C");

                _logRows = SqlTestHelpers.SimpleQuery(conn, "SELECT * FROM log").Rows.Select(_ => new logEntry(_)).ToList();
                var lookup = _logRows.ToLookup(_ => _.SessionId, _ => _);
                _log1 = lookup.FirstOrDefault(_ => _.Count() == 3)?.ToList();
                _log2 = lookup.FirstOrDefault(_ => _.Count() == 6)?.ToList();
                _log3 = lookup.FirstOrDefault(_ => _.Count() == 1)?.ToList();
                _log1?.Sort((x, y) => x.SeqId.CompareTo(y.SeqId));
                _log2?.Sort((x, y) => x.SeqId.CompareTo(y.SeqId));
                _log3?.Sort((x, y) => x.SeqId.CompareTo(y.SeqId));
            });
        }

        [Test]
        public void ThenThereAreThreeSessionsInTheLogTable()
        {
            if (SqlTestHelpers.ConnectionString(null) == null)
                return;
            var lookup = _logRows.ToLookup(_ => _.SessionId, _ => _);
            Assert.AreEqual(3, lookup.Count);
        }

        [Test]
        public void ThenThereAreTenRowsInTheLogTable()
        {
            if (SqlTestHelpers.ConnectionString(null) == null)
                return;
            var lookup = _logRows.ToLookup(_ => _.SessionId, _ => _);
            Assert.AreEqual(10, _logRows.Count);
        }

        [Test]
        public void ThenThereAreSequenceIds()
        {
            if (SqlTestHelpers.ConnectionString(null) == null)
                return;
            Assert.AreEqual(0, _logRows.Min(_ => _.SeqId));
            Assert.AreEqual(5, _logRows.Max(_ => _.SeqId));
        }

        [Test]
        public void ThenThereAreTimeStamps()
        {
            if (SqlTestHelpers.ConnectionString(null) == null)
                return;
            Assert.IsTrue(_logRows.TrueForAll(_ => Math.Abs((DateTime.Now - _.When).TotalSeconds) < 15));
        }

        [Test]
        public void ThenTheFirstSessionContainsTheCorrectRows()
        {
            if (SqlTestHelpers.ConnectionString(null) == null)
                return;
            CollectionAssert.AreEqual(new[]
            {
                "CREATE TABLE [School_Classes_Students_tmp] ([School_Classes_id_] uniqueidentifier not null,[FirstName] nvarchar(max),[LastName] nvarchar(max));CREATE TABLE [School_Classes_tmp] ([_id_] uniqueidentifier not null,[School_id_] uniqueidentifier not null,[Name] nvarchar(max));CREATE TABLE [School_tmp] ([_id_] uniqueidentifier not null,[Name] nvarchar(max))",
                "EXEC sp_rename 'School_Classes_Students_tmp', 'School_Classes_Students';EXEC sp_rename 'School_Classes_tmp', 'School_Classes';EXEC sp_rename 'School_tmp', 'School'",
                "Verified table row counts: School_Classes_Students:100;School_Classes:6;School:1"
            }, _log1.Select(_ => _.Text));
        }

        [Test]
        public void ThenTheSecondSessionContainsTheCorrectRows()
        {
            if (SqlTestHelpers.ConnectionString(null) == null)
                return;
            CollectionAssert.AreEqual(new[]
            {
                "Explicit logging from the outside A",
                "CREATE TABLE [School_Classes_Students_tmp] ([School_Classes_id_] uniqueidentifier not null,[FirstName] nvarchar(max),[LastName] nvarchar(max));CREATE TABLE [School_Classes_tmp] ([_id_] uniqueidentifier not null,[School_id_] uniqueidentifier not null,[Name] nvarchar(max));CREATE TABLE [School_tmp] ([_id_] uniqueidentifier not null,[Name] nvarchar(max))",
                "EXEC sp_rename 'School_Classes_Students', 'School_Classes_Students_bck';EXEC sp_rename 'School_Classes', 'School_Classes_bck';EXEC sp_rename 'School', 'School_bck'",
                "EXEC sp_rename 'School_Classes_Students_tmp', 'School_Classes_Students';EXEC sp_rename 'School_Classes_tmp', 'School_Classes';EXEC sp_rename 'School_tmp', 'School'",
                "Verified table row counts: School_Classes_Students:100;School_Classes:6;School:1",
                "Explicit logging from the outside B"
            }, _log2.Select(_ => _.Text));
        }

        [Test]
        public void ThenTheExplicitSessionContainsTheCorrectRows()
        {
            if (SqlTestHelpers.ConnectionString(null) == null)
                return;
            CollectionAssert.AreEqual(new[]
            {
                "Explicit logging from the outside C"
            }, _log3.Select(_ => _.Text));
        }

    }

}