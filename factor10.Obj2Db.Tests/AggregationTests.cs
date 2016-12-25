using System.Collections.Generic;
using System.Linq;
using factor10.Obj2Db.Tests.TestData;
using factor10.Obj2Db.Tests.TestDataStructures;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests
{
    [TestFixture]
    public class AggregationTests
    {
        private List<ITable> _tables; 
        private ITable _topTable;
        private ITable _selfListTable;

        [OneTimeSetUp]
        public void Test()
        {
            var spec = entitySpec.Begin()
                .Add("FirstName")
                .Add("AggregatedX").Aggregates("Structs.X")
                .Add(entitySpec.Begin("Structs").NotSaved()
                    .Add("X"))
                .Add(entitySpec.Begin("Self.SelfList")
                    .Add("Double"))
                .Add("AggregatedDouble").Aggregates("Self.SelfList.Double")
                ;

            var theTop = new TheTop
            {
                FirstName = "Petronella",
                Structs = Enumerable.Range(1, 4).Select(_ => new SomeStruct {X = _}).ToList(),
                SomeStruct = new SomeStruct {X = 7, Y = 8},
                Self = new TheTop
                {
                    SelfList = new List<TheTop> {new TheTop {Double = 42}, new TheTop {Double = 43}}
                }
            };
            var t = new InMemoryTableManager();
            var export = new Export<TheTop>(spec, t);
            export.Run(theTop);
            _tables = export.TableManager.GetWithAllData();

            _topTable = _tables.Single(_ => _.Name == "TheTop");
            _selfListTable = _tables.Single(_ => _.Name == "SelfSelfList");
        }

        [Test]
        public void TestThatTheTablesAreCorrect()
        {
            CollectionAssert.AreEqual(new[] {"TheTop", "SelfSelfList"}, _tables.Select(_ => _.Name));
        }

        [Test]
        public void TestThatAllFieldsAreInToTable()
        {
            CollectionAssert.AreEqual(new[] {"FirstName", "AggregatedX", "AggregatedDouble"}, _topTable.Fields.Select(_ => _.Name));
        }

        [Test]
        public void TestThatXAggregatedCorrectly()
        {
            Assert.AreEqual(1 + 2 + 3 +4, _topTable.Rows.Single().Columns[1]);
        }

        [Test]
        public void TestThatXAggregatedTypeIsCorrect()
        {
            Assert.AreEqual("Int32", _topTable.Rows.Single().Columns[1].GetType().Name);
        }

        [Test]
        public void TestThatDoubleAggregatedCorrectly()
        {
            Assert.AreEqual(42 + 43, _topTable.Rows.Single().Columns[2]);
        }

        [Test]
        public void TestThatDoubleAggregatedTypeIsCorrect()
        {
            Assert.AreEqual("Double", _topTable.Rows.Single().Columns[2].GetType().Name);
        }

    }

    public class Aggregation2Tests
    {
        private List<ITable> _tables;
        private ITable _topTable;

        [OneTimeSetUp]
        public void Test()
        {
            var spec = entitySpec.Begin()
                .Add(entitySpec.Begin("SelfList").NotSaved()
                    .Add("Double"))
                .Add("AggregatedDouble").Aggregates("SelfList.Double")
                ;

            var theTop = new TheTop
            {
                SelfList = new List<TheTop> {new TheTop {Double = 42}, new TheTop {Double = 43}}
            };
            var t = new InMemoryTableManager();
            var export = new Export<TheTop>(spec, t);
            export.Run(theTop);
            _tables = export.TableManager.GetWithAllData();

            _topTable = _tables.Single(_ => _.Name == "TheTop");
        }

        [Test]
        public void TestThatTheTablesAreCorrect()
        {
            CollectionAssert.AreEqual(new[] { "TheTop" }, _tables.Select(_ => _.Name));
        }

        [Test]
        public void TestThatAllFieldsAreInToTable()
        {
            CollectionAssert.AreEqual(new[] { "AggregatedDouble" }, _topTable.Fields.Select(_ => _.Name));
        }

        [Test]
        public void TestThatDoubleAggregatedCorrectly()
        {
            Assert.AreEqual(42 + 43, _topTable.Rows.Single().Columns.Single());
        }

        [Test]
        public void TestThatDoubleAggregatedTypeIsCorrect()
        {
            Assert.AreEqual("Double", _topTable.Rows.Single().Columns.Single().GetType().Name);
        }

    }

    [TestFixture]
    public class TestFormulaAndNoSaveAndDictionary
    {
        [Test]
        public void Test()
        {
            var spec = entitySpec.Begin()
                .Add(entitySpec.Begin("DictionariesAreSneaky")
                    .Add("KeyValue").Formula("Key+Value")
                    .Add("Key").NotSaved()
                    .Add("Value").NotSaved());
            var x = new TestClassWithSneakyStuff
            {
                DictionariesAreSneaky = new Dictionary<int, int>
                    {{1, 99}, {5, 15}}
            };

            var export = new Export<TestClassWithSneakyStuff>(spec);
            export.Run(x);

            var t = export.TableManager.GetWithAllData().Single(_ => _.Name== "DictionariesAreSneaky");
            CollectionAssert.AreEquivalent(new[] {100, 20}, t.Rows.Select(_ => _.Columns.Single()));
        }   

    }

    public class Nisse
    {
        public List<int> List1;
        public List<int> List2;
        public List<List<int>> List3;
    }

    [TestFixture]
    public class TestGurka
    {
        [Test]
        public void TestThatAggregatedValuesCanBeUsedInFormulas()
        {
            var spec = entitySpec.Begin()
                .Add("SumList1").Aggregates("List1.value").NotSaved()
                .Add("SumList2").Aggregates("List2.value").NotSaved()
                .Add("Average").Formula("(SumList1+SumList2)/2")
                .Add("List1")
                .Add("List2")
                .Add("List3");
            var x = new Nisse
            {
                List1 = new List<int> {5, 6, 7},
                List2 = new List<int> {20},
                List3 = new List<List<int>> {new List<int> {15}, new List<int> { 15,16,17 }, new List<int> { 18 } }
            };

            var export = new Export<Nisse>(spec);
            export.Run(x);

            var t = export.TableManager.GetWithAllData().First();

            CollectionAssert.AreEqual(new[] {19}, t.Rows.Single().Columns);
        }

    }

    [TestFixture]
    public class TestGurka2
    {
        [Test]
        public void TestThatAggregatedValuesCanBeUsedInFormulas()
        {
            var spec = entitySpec.Begin()
                .Add(entitySpec.Begin("List3")
                    ); //.Add("."));
            var x = new Nisse
            {
                List3 = new List<List<int>> { new List<int> { 15 }, new List<int> { 15, 16, 17 }, new List<int> { 18 } }
            };

            var export = new Export<Nisse>(spec);
            export.Run(x);

            var t = export.TableManager.GetWithAllData().First();

            CollectionAssert.AreEqual(new[] { 19 }, t.Rows.Single().Columns);
        }

    }

}
