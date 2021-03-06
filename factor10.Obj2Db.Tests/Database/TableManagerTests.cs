﻿using System;
using System.Data.SqlClient;
using factor10.Obj2Db.Tests.TestData;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests.Database
{
    [TestFixture]
    public class TableManagerTests : SchoolBaseTests
    {
        [Test]
        public void TestThatInMemoryTableDoesntAllowDuplicateColumnNames()
        {
            var spec = new entitySpec()
                .Add("Name")
                .Add("Name");
            var entity = Entity.Create(null, spec, typeof(School), null);
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                new InMemoryTable(entity, true, true, -1);
            });
            Assert.AreEqual("Table 'School' contains duplicate column names 'NAME'", ex.Message);
        }

        [Test]
        public void TestTableProgression()
        {
            SqlTestHelpers.WithNewDb("SqlTableProgression", conn =>
            {
                using (var cmd = new SqlCommand("CREATE TABLE School_bck (x int)", conn))
                    cmd.ExecuteNonQuery();
                using (var cmd = new SqlCommand("CREATE TABLE School_Classes (x int)", conn))
                    cmd.ExecuteNonQuery();
                using (var cmd = new SqlCommand("CREATE TABLE School_Classes_Students_tmp (x int)", conn))
                    cmd.ExecuteNonQuery();

                var tableManager = new SqlTableManager(SqlTestHelpers.ConnectionString("SqlTableProgression"));
                var dataExtract = new DataExtract<School>(Spec, tableManager);
                var entityWithTable = new EntityWithTable(dataExtract.TopEntity, tableManager);

                // just to make some use of entityWithTable
                Assert.IsNotNull(entityWithTable);

                // pre-assert
                var tables = tableManager.GetExistingTableNames(conn, false);
                CollectionAssert.AreEquivalent(new[] { "School_bck", "School_Classes", "School_Classes_Students_tmp" }, tables);

                // after first begin, we should have a full set of _tmp, nothing else touched
                tableManager.Begin();
                tables = tableManager.GetExistingTableNames(conn, false);
                CollectionAssert.AreEquivalent(new[] {"School_bck", "School_Classes", "School_Classes_Students_tmp", "School_tmp", "School_Classes_tmp" }, tables);

                // after first end, we should have a _bck of the faked School_Classes and the three real tables and no _tmp
                tableManager.End();
                tables = tableManager.GetExistingTableNames(conn, false);
                CollectionAssert.AreEquivalent(new[] { "School_Classes_bck", "School", "School_Classes", "School_Classes_Students" }, tables);

                // after second begin, we should have a full set of _tmp, nothing else touched since last check
                tableManager.Begin();
                tables = tableManager.GetExistingTableNames(conn, false);
                CollectionAssert.AreEquivalent(new[] { "School_Classes_bck", "School", "School_Classes", "School_Classes_Students", "School_Classes_Students_tmp", "School_tmp", "School_Classes_tmp" }, tables);

                // efter second, we should have a full set of _bck and and real files and no _tmp
                tableManager.End();
                tables = tableManager.GetExistingTableNames(conn, false);
                CollectionAssert.AreEquivalent(new[] { "School_bck", "School_Classes_bck", "School_Classes_Students_bck", "School", "School_Classes", "School_Classes_Students" }, tables);

            });
        }

        [Test]
        public void TestThatWrongNumberOfRowsIsCaught()
        {
            SqlTestHelpers.WithNewDb("SqlTableSimulateBadWrite", conn =>
            {
                var tableManager = new SqlTableManager(SqlTestHelpers.ConnectionString("SqlTableSimulateBadWrite"));
                var dataExtract = new DataExtract<School>(Spec, tableManager);
                var entityWithTable = new EntityWithTable(dataExtract.TopEntity, tableManager);

                // just to make some use of entityWithTable
                Assert.IsNotNull(entityWithTable);

                // after first begin, we should have a full set of _tmp, nothing else touched
                tableManager.Begin();

                using (var cmd = new SqlCommand($"INSERT INTO school_tmp (_id_,name) VALUES ('{Guid.NewGuid()}','nisse')", conn))
                    cmd.ExecuteNonQuery();

                var ex = Assert.Throws<Exception>(() => tableManager.End());
                Assert.AreEqual("Found 1 rows in table School but expected 0", ex.Message);
            });
        }

    }
}
