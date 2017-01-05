using System.Collections.Generic;
using System.Linq;
using factor10.Obj2Db.Tests.TestData;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests
{
    [TestFixture]
    public class SchoolAggregationTests : SchoolBaseTests
    {
        private readonly entitySpec _spec = entitySpec.Begin()
            .Add("Name")
            .Add(entitySpec.Begin("Classes")
                .Add("Name")
                .Add("StudentCount").Aggregates("Students", "count")
                .Add("Students").NotSaved()
                );

        private ITable _classesTable;

        [OneTimeSetUp]
        public void TestSchoolExample()
        {
            var export = new DataExtract<School>(_spec);
            export.Run(School);
            var tables = export.TableManager.GetWithAllData();
            Assert.AreEqual(2, tables.Count);
            _classesTable = tables.Single(_ => _.Name == "Classes");
        }

        [Test]
        public void TestThatThereAreSixClasses()
        {
            Assert.AreEqual(6, _classesTable.Rows.Count);
        }

        [Test]
        public void TestThatThereAreTwoColumns()
        {
            Assert.AreEqual(2, _classesTable.Fields.Count);
        }

        [Test]
        public void TestThatThereAreATotalOf100Students()
        {
            Assert.AreEqual(100, _classesTable.Rows.Sum(_ => (int)_.Columns[1]));
        }

    }

}