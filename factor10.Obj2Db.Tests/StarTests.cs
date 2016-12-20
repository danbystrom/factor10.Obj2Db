using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using factor10.Obj2Db.Tests.TestData;
using factor10.Obj2Db.Tests.TestDataStructures;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests
{
    [TestFixture]
    public class BasicStarTests
    {
        private Entity _entity;

        [OneTimeSetUp]
        public void SetUp()
        {
            var spec = EntitySpec.Begin().Add("*");
            var export = new Export<School>(spec);
            _entity = export.Entity;
        }

        [Test]
        public void TestThatTopLevelHasName()
        {
            var nameProp = _entity.Fields.Single();
            Assert.IsTrue(nameProp.Name == "Name");
            Assert.IsTrue(nameProp.FieldInfo.FieldType == typeof (string));
        }

        [Test]
        public void TestThatTopLevelHasClasses()
        {
            var nameProp = _entity.Lists.Single();
            Assert.IsTrue(nameProp.Name == "Classes");
            Assert.IsNotNull(LinkedFieldInfo.CheckForIEnumerable(nameProp.FieldInfo.FieldType));
        }

        [Test]
        public void TestThatClassLevelHasName()
        {
            var nameProp = _entity.Lists.Single().Fields.Single();
            Assert.IsTrue(nameProp.Name == "Name");
            Assert.IsTrue(nameProp.FieldInfo.FieldType == typeof(string));
        }

        [Test]
        public void TestThatClassLevelHasStudents()
        {
            var nameProp = _entity.Lists.Single().Lists.Single();
            Assert.IsTrue(nameProp.Name == "Students");
            Assert.IsNotNull(LinkedFieldInfo.CheckForIEnumerable(nameProp.FieldInfo.FieldType));
        }

        [Test]
        public void TestThatStudentLevelHasFirstAndLastNames()
        {
            var props = _entity.Lists.Single().Lists.Single().Fields;
            CollectionAssert.AreEquivalent(new[] {"FirstName", "LastName"}, props.Select(_ => _.Name));
            CollectionAssert.AreEquivalent(new[] {"String", "String" }, props.Select(_ => _.FieldType.Name));
        }

        [Test]
        public void TestThatStudentLevelHaveNoSubEntities()
        {
            var sub = _entity.Lists.Single().Lists.Single().Lists;
            Assert.IsFalse(sub.Any());
        }


    }

    public class MoreStarTests
    {
        private Entity _entity;
        private ITable _topTable;
        private ITable _subTable;

        [OneTimeSetUp]
        public void SetUp()
        {
            var spec = EntitySpec.Begin().Add("*");
            var tm = new InMemoryTableManager();
            var export = new Export<TestClassWithSneakyStuff>(spec, tm);
            _entity = export.Entity;

            var tcwss = new TestClassWithSneakyStuff
            {
                TheStruct = new SomeStruct {X = 12, Y = 19},
                TheList = new SneakyList {NonObviousProperty = 56}
            };
            tcwss.TheList.AddRange(new[] {"Straight", "to", "the", "heart"});
            export.Run(tcwss);
            var table = export.TableManager.GetWithAllData();
            _topTable = table.Single(_ => _.Name == "TestClassWithSneakyStuff");
            _subTable = table.Single(_ => _.Name == "TheList");
        }

        [Test]
        public void TestThatTheTopTableHasCorrectData()
        {
            CollectionAssert.AreEqual(new[] {12, 19,  56 }, _topTable.Rows.Single().Columns);
        }

        [Test]
        public void TestThatTheSubTableHasCorrectData()
        {
            CollectionAssert.AreEquivalent(new[] { "Straight", "to", "the", "heart" }, _subTable.Rows.Select(_ => _.Columns.Single()));
        }

    }

    public class DeepStarTests
    {
        private Entity _entity;
        private ITable _topTable;

        [OneTimeSetUp]
        public void SetUp()
        {
            var spec = EntitySpec.Begin().Add("*");
            var tm = new InMemoryTableManager();
            var export = new Export<DeepDeclaration>(spec, tm);
            _entity = export.Entity;

            var dd = new DeepDeclaration
            {
                TheFirst = new Level2 {X1 = new Level3 {Ss1 = new SomeStruct {X = 78}}},
                TheSecond = new Level2 {X2 = new Level3 {Ss2 = new SomeStruct {Y = 79}}},
            };
            export.Run(dd);
            var table = export.TableManager.GetWithAllData();
            _topTable = table.Single();
        }

        [Test]
        public void Test()
        {
            CollectionAssert.AreEqual(new[]
            {
                "TheFirst.X1.Ss1.X",
                "TheFirst.X1.Ss1.Y",
                "TheFirst.X1.Ss2.X",
                "TheFirst.X1.Ss2.Y",
                "TheFirst.X2.Ss1.X",
                "TheFirst.X2.Ss1.Y",
                "TheFirst.X2.Ss2.X",
                "TheFirst.X2.Ss2.Y",
                "TheSecond.X1.Ss1.X",
                "TheSecond.X1.Ss1.Y",
                "TheSecond.X1.Ss2.X",
                "TheSecond.X1.Ss2.Y",
                "TheSecond.X2.Ss1.X",
                "TheSecond.X2.Ss1.Y",
                "TheSecond.X2.Ss2.X",
                "TheSecond.X2.Ss2.Y",
            }, _entity.Fields.Select(_ => _.Name));
        }

        [Test]
        public void TestThatTheTopTableHasCorrectData()
        {
            CollectionAssert.AreEqual(new object[] { 78, 0, 0, 0, null, null, null, null, null, null, null, null, 0, 0, 0, 79}, _topTable.Rows.Single().Columns);
        }

        [Test]
        public void TestThatTheSubTableHasCorrectData()
        {
        }

    }

}
