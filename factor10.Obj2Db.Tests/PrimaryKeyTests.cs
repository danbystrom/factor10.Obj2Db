using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using factor10.Obj2Db.Tests.Database;
using factor10.Obj2Db.Tests.TestData;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests
{
    [TestFixture]
    public class PrimaryKeyTests : SchoolBaseTests
    {
        private QueryResult _classQueryResult;

        [OneTimeSetUp]
        public void SetUp2()
        {
            Spec = entitySpec.Begin()
                .Add("Name")
                .Add(entitySpec.Begin("Classes")
                    .Add("Name").PrimaryKey()
                    .Add(entitySpec.Begin("Students")
                        .Add("FirstName")
                        .Add("LastName")));

            var exportMem = new DataExtract<School>(Spec);
            exportMem.Run(School);
            var tables = exportMem.TableManager.GetWithAllData();
            Assert.AreEqual(1, tables.Single(_ => _.Name == "School").Rows.Count);
            Assert.AreEqual(6, tables.Single(_ => _.Name == "Classes").Rows.Count);
            Assert.AreEqual(100, tables.Single(_ => _.Name == "Students").Rows.Count);

            if (Environment.MachineName != "DAN_FACTOR10")
                return;
            var tableFactory = new SqlTableManager(SqlTestHelpers.ConnectionString("SchoolPkTest"));
            var exportDb = new DataExtract<School>(Spec, tableFactory);
            SqlTestHelpers.WithNewDb("SchoolPkTest", conn =>
            {
                var sw = Stopwatch.StartNew();
                exportDb.Run(Enumerable.Range(0, 1).Select(_ => School));
                _classQueryResult = SqlTestHelpers.SimpleQuery(conn, "SELECT * FROM school_classes");
            });

        }

        [Test]
        public void TestThat()
        {
            Assert.AreEqual(2, _classQueryResult.NameAndTypes.Length);
        }

    }
}
