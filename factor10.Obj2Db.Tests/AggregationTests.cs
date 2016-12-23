﻿using System.Collections.Generic;
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
                .Add(entitySpec.Begin("SelfList")
                    .Add("Double"))
                .Add("AggregatedDouble").Aggregates("SelfList.Double")
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
            _selfListTable = _tables.Single(_ => _.Name == "SelfList");
        }

        [Test]
        public void TestThatTheTablesAreCorrect()
        {
            CollectionAssert.AreEqual(new[] { "TheTop","SelfList" }, _tables.Select(_ => _.Name));
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

}
