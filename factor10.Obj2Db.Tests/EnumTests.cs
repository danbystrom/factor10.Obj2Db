using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using factor10.Obj2Db.Tests.TestDataStructures;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests
{
    [TestFixture]
    public class EnumTests
    {
        private readonly entitySpec _spec = entitySpec.Begin()
                .Add("Case1")
                .Add("Case2")
                .Add("Case3")
                .Add("Case4")
                .Add("Case5")
                .Add("Case6")
                .Add("DeeperF.Case1")
                .Add("DeeperF.Case2")
                .Add("DeeperF.Case3")
                .Add("DeeperF.Case4")
                .Add("DeeperF.Case5")
                .Add("DeeperF.Case6")
                .Add("DeeperP.Case1")
                .Add("DeeperP.Case2")
                .Add("DeeperP.Case3")
                .Add("DeeperP.Case4")
                .Add("DeeperP.Case5")
                .Add("DeeperP.Case6")
                .Add("DeeperF.DeeperP.Case1")
                .Add("DeeperP.DeeperF.Case1");

        [Test]
        public void TestWithAllNulls()
        {
            var export = new DataExtract<ClassToTestEnumerables>(_spec);
            export.Run(new ClassToTestEnumerables());
        }

        [Test]
        public void TestWithAll()
        {
            var c = new ClassToTestEnumerables
            {
                Case1 = new[] {1, 2},
                Case2 = new[] {3, 4},
                Case3 = new List<int> {5, 6},
                Case4 = new[] {7, 8},
                Case5 = new[] {9, 10},
                Case6 = new List<int> {11, 12}
            };
            c.DeeperF = c;
            c.DeeperP = c;
            var t = new InMemoryTableManager();
            var export = new DataExtract<ClassToTestEnumerables>(_spec, t);
            export.Run(c);
            var tables = export.TableManager.GetWithAllData().ToDictionary(_ => _.Name, _ => _.Rows);
            tables.Remove("ClassToTestEnumerables");
            Assert.IsTrue(tables.Values.All(_ => _.Count == 2));
        }

    }

    [TestFixture]
    public class TestListOfListInts
    {
        private Dictionary<string, ITable> _tables;

        [OneTimeSetUp]
        public void TestThatAggregatedValuesCanBeUsedInFormulas()
        {
            var spec = entitySpec.Begin(null, "ontop")
                //.Add("SumList3").Aggregates("List3.").NotSaved()
                .Add(entitySpec.Begin("List3")
                    .Add(entitySpec.Begin("@", "innerlist")
                        .Add("@|zvalue")));
            var x = new Nisse
            {
                List3 = new List<List<int>> { new List<int> { 15 }, new List<int> { 15, 16, 17 }, new List<int> { 18 } }
            };

            var sb = new StringBuilder();
            Action<string> log = _ => sb.AppendLine(_);
            var export = new DataExtract<Nisse>(spec, null, log);
            export.Run(x);

            _tables = export.TableManager.GetWithAllData().ToDictionary(_ => _.Name, _ => _);
        }

        [Test]
        public void TestThatTableNamesAreCorrect()
        {
            CollectionAssert.AreEquivalent(new[] { "ontop", "List3", "innerlist" }, _tables.Keys);
        }

        [Test]
        public void TestThatListRowsAreCorrectlyDistributed()
        {
            var x = _tables["innerlist"].Rows.ToLookup(_ => _.ParentRow, _ => _.Columns.Single());
            CollectionAssert.AreEquivalent(new[]
            {
                new[] {15},
                new[] {15, 16, 17},
                new[] {18}
            }, x);
        }

    }

}
