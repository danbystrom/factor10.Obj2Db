using System.Collections.Generic;
using System.Linq;
using factor10.Obj2Db.Tests.TestData;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests
{
    [TestFixture]
    public class FormulaTests
    {
        [Test]
        public void TestSimpleFormula()
        {
            var spec = entitySpec.Begin()
                .Add("Double")
                .Add("kalle").Formula("3+Double")
                .Add("nisse").Formula("5*6")
                .Add("sture").Formula("kalle+nisse");

            var export = new DataExtract<TheTop>(spec);
            export.Run(new TheTop {Double = 4});
            CollectionAssert.AreEqual(new[] {4.0, 7.0, 30.0, 37.0}, export.TableManager.GetWithAllData().Single().Rows.Single().Columns);
        }

    }

    [TestFixture]
    public class AutonumberingTests
    {
        [Test]
        public void TestAutonumbering()
        {
            var spec = entitySpec.Begin()
                .Add(entitySpec.Begin("Strings")
                .Add("x").Formula("#index*2")
                .Add("@|y"));

            var export = new DataExtract<TheTop>(spec);
            export.Run(new TheTop {Strings = new List<string> {"a", "b", "c", "d", "e"}});

            var rows = export.TableManager.GetWithAllData().Single(_ => _.Name=="Strings").Rows;
            var firstColumn = rows.Select(_ => _.Columns.First());
            var lastColumn = rows.Select(_ => _.Columns.Last());

            CollectionAssert.AreEqual(new[] { 0,2,4,6,8 }, firstColumn);
            CollectionAssert.AreEqual(new[] { "a", "b", "c", "d", "e" }, lastColumn);
        }

        [Test]
        public void TestAutonumberingWithAggregation()
        {
            var spec = entitySpec.Begin()
                .Add("Tot").Aggregates("Strings.x")
                .Add(entitySpec.Begin("Strings")
                .Add("x").Formula("#index*2")
                .Add("@|y"));

            var export = new DataExtract<TheTop>(spec);
            export.Run(new TheTop { Strings = new List<string> { "a", "b", "c", "d", "e" } });
            var tables = export.TableManager.GetWithAllData();

            var rows = tables.Single(_ => _.Name == "Strings").Rows;
            var firstColumn = rows.Select(_ => _.Columns.First());
            var lastColumn = rows.Select(_ => _.Columns.Last());

            CollectionAssert.AreEqual(new[] { 0, 2, 4, 6, 8 }, firstColumn);
            CollectionAssert.AreEqual(new[] { "a", "b", "c", "d", "e" }, lastColumn);

            Assert.AreEqual(2 + 4 + 6 + 8, tables.First().Rows.Single().Columns.Single());
        }

    }

}
