using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
            var export = new DataExtract<TheTop>(spec, t);
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
            Assert.AreEqual(1 + 2 + 3 + 4, _topTable.Rows.Single().Columns[1]);
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
            var export = new DataExtract<TheTop>(spec, t);
            export.Run(theTop);
            _tables = export.TableManager.GetWithAllData();

            _topTable = _tables.Single(_ => _.Name == "TheTop");
        }

        [Test]
        public void TestThatTheTablesAreCorrect()
        {
            CollectionAssert.AreEqual(new[] {"TheTop"}, _tables.Select(_ => _.Name));
        }

        [Test]
        public void TestThatAllFieldsAreInToTable()
        {
            CollectionAssert.AreEqual(new[] {"AggregatedDouble"}, _topTable.Fields.Select(_ => _.Name));
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

            var export = new DataExtract<TestClassWithSneakyStuff>(spec);
            export.Run(x);

            var t = export.TableManager.GetWithAllData().Single(_ => _.Name == "DictionariesAreSneaky");
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
                .Add("SumList1").Aggregates("List1.@").NotSaved()
                .Add("SumList2").Aggregates("List2.@").NotSaved()
                .Add("Average").Formula("(SumList1+SumList2)/2")
                .Add("List1")
                .Add("List2")
                .Add(entitySpec.Begin("List3")
                    .Add(entitySpec.Begin("@", "q")
                        .Add("@|z")));
            var x = new Nisse
            {
                List1 = new List<int> {5, 6, 7},
                List2 = new List<int> {20},
                List3 = new List<List<int>> {new List<int> {15}, new List<int> {15, 16, 17}, new List<int> {18}}
            };

            var export = new DataExtract<Nisse>(spec);
            export.Run(x);

            var t = export.TableManager.GetWithAllData().First();

            CollectionAssert.AreEqual(new[] {19}, t.Rows.Single().Columns);
        }

    }

    [TestFixture]
    public class TestGurka2
    {
        private ITable _topTable;
        private ITable _subTable;

        [OneTimeSetUp]
        public void TestThatAggregatedValuesCanBeUsedInFormulas()
        {
            var spec = entitySpec.Begin(null, "Tjo")
                .Add("squash").Aggregates("List1.@")
                .Add(entitySpec.Begin("List1", "Hopp")
                    .Add("@|gurka"));
            var x = new Nisse
            {
                List1 = new List<int> {5, 6, 7},
            };

            var export = new DataExtract<Nisse>(spec);
            export.Run(x);

            var tables = export.TableManager.GetWithAllData();
            _topTable = tables.First();
            _subTable = tables.Last();
        }

        [Test]
        public void TestThatTopTableHasCorrectName()
        {
            Assert.AreEqual("Tjo", _topTable.Name);
        }

        [Test]
        public void TestThatSubTableHasCorrectName()
        {
            Assert.AreEqual("Hopp", _subTable.Name);
        }

        [Test]
        public void TestThatTopTableHasCorrectColumnNames()
        {
            CollectionAssert.AreEqual(new[] {"squash"}, _topTable.Fields.Select(_ => _.Name));
        }

        [Test]
        public void TestThatSubTableHasCorrectColumnNames()
        {
            CollectionAssert.AreEqual(new[] {"gurka"}, _subTable.Fields.Select(_ => _.Name));
        }

        [Test]
        public void TestThatTopTableHasCorrectColumnValues()
        {
            CollectionAssert.AreEqual(new[] {5 + 6 + 7}, _topTable.Rows.Single().Columns);
        }

        [Test]
        public void TestThatSubTableHasCorrectColumnValues()
        {
            CollectionAssert.AreEquivalent(new[] {5, 6, 7}, _subTable.Rows.Select(_ => _.Columns.Single()));
        }

    }

    [TestFixture]
    public class TestAggregationOfListOfListInts
    {
        private Dictionary<string, ITable> _tables;

        [OneTimeSetUp]
        public void TestThatAggregatedValuesCanBeUsedInFormulas()
        {
            var spec = entitySpec.Begin(null, "ontop")
                .Add("bigzum").Aggregates("List3.zum")
                .Add(entitySpec.Begin("List3")
                    .Add("zum").Aggregates("@.@")
                    .Add(entitySpec.Begin("@", "innerlist")
                        .Add("@|zvalue")));
            var x = new Nisse
            {
                List3 = new List<List<int>> {new List<int> {15}, new List<int> {15, 16, 17}, new List<int> {18}}
            };

            var sb = new StringBuilder();
            Action<string> log = _ => sb.AppendLine(_);
            var export = new DataExtract<Nisse>(spec, null, log);
            export.Run(x);

            _tables = export.TableManager.GetWithAllData().ToDictionary(_ => _.Name, _ => _);
        }

        [Test]
        public void TestThatItSummedAllTheWayUp()
        {
            Assert.AreEqual(15 + 15 + 16 + 17 + 18, _tables["ontop"].Rows.Single().Columns.Single());
        }

    }

    [TestFixture]
    public class TestAllAggregationTypes
    {
        private readonly entitySpec _spec = entitySpec.Begin()
            .Add("SumList1").Aggregates("List1.@")
            .Add("SumList2").Aggregates("List2.@")
            .Add("SumList1_").Aggregates("List1.@", "")
            .Add("SumList2_").Aggregates("List2.@", "")
            .Add("MaxList1").Aggregates("List1.@", "max")
            .Add("MaxList2").Aggregates("List2.@", "max")
            .Add("MinList1").Aggregates("List1.@", "min")
            .Add("MinList2").Aggregates("List2.@", "min")
            .Add("MinList1").Aggregates("List1.@", "avg")
            .Add("MinList2").Aggregates("List2.@", "avg")
            .Add("MinList1").Aggregates("List1.@", "count")
            .Add("MinList2").Aggregates("List2.@", "count")
            .Add(entitySpec.Begin("List1").Where("@!=7"))
            .Add(entitySpec.Begin("List2").Where("@!=7"));

        [Test]
        public void BasicTestOfAllAggregationTypes()
        {
            var x = new Nisse
            {
                List1 = new List<int> {5, 6, 7},
                List2 = new List<int> {7, 15, 18, 20},
            };

            var export = new DataExtract<Nisse>(_spec);
            export.Run(x);

            var t = export.TableManager.GetWithAllData().First();

            CollectionAssert.AreEqual(new object[]
            {
                5 + 6,
                15 + 18 + 20,
                5 + 6,
                15 + 18 + 20,
                6,
                20,
                5,
                15,
                LinkedFieldInfo.CoherseType(typeof(int), (5 + 6) / 2.0),
                LinkedFieldInfo.CoherseType(typeof(int), (15 + 18 + 20) / 3.0),
                2,
                3
            }, t.Rows.Single().Columns);
        }

        [Test]
        public void TestThatAggregationWorksWithEmptyLists()
        {
            var x = new Nisse
            {
                List1 = new List<int>(),
                List2 = new List<int> {7}
            };

            var export = new DataExtract<Nisse>(_spec);
            export.Run(x);

            var t = export.TableManager.GetWithAllData().First();

            CollectionAssert.AreEqual(new object[]
            {
                0,
                0,
                0,
                0,
                null,
                null,
                null,
                null,
                null,
                null,
                0,
                0
            }, t.Rows.Single().Columns);
        }

    }

    [TestFixture]
    public class TestAllAggregationTypesWithFormulas
    {
        private readonly entitySpec _spec = entitySpec.Begin()
            .Add("SumList1").Aggregates("List1.@").Formula("@*2")
            .Add("SumList2").Aggregates("List2.@").Formula("@??42")
            .Add("SumList1_").Aggregates("List1.@", "").Formula("@??42")
            .Add("SumList2_").Aggregates("List2.@", "").Formula("@??43")
            .Add("MaxList1").Aggregates("List1.@", "max").Formula("@??44")
            .Add("MaxList2").Aggregates("List2.@", "max").Formula("@??45")
            .Add("MinList1").Aggregates("List1.@", "min").Formula("@??46")
            .Add("MinList2").Aggregates("List2.@", "min").Formula("@??47")
            .Add("MinList1").Aggregates("List1.@", "avg").Formula("@??48")
            .Add("MinList2").Aggregates("List2.@", "avg").Formula("@??49")
            .Add("MinList1").Aggregates("List1.@", "count").Formula("@*2")
            .Add("MinList2").Aggregates("List2.@", "count").Formula("@??50")
            .Add(entitySpec.Begin("List1").Where("@!=7"))
            .Add(entitySpec.Begin("List2").Where("@!=7"));

        [Test]
        public void BasicTestOfAllAggregationTypes()
        {
            var x = new Nisse
            {
                List1 = new List<int> { -1 },
                List2 = null
            };

            var export = new DataExtract<Nisse>(_spec);
            export.Run(x);

            var t = export.TableManager.GetWithAllData().First();
            var column = t.Rows.Single().Columns;
            CollectionAssert.AreEqual(new object[]
            {
                -1  * 2,
                0,
                -1,
                0,
                -1,
                45,
                -1,
                47,
                -1,
                49,
                2,
                0
            }, column);
        }

    }

}
