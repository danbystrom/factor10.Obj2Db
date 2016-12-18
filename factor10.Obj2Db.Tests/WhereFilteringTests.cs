using System.Collections.Generic;
using System.Linq;
using factor10.Obj2Db.Tests.TestData;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests
{

    [TestFixture]
    public class WhereFilteringTests
    {
        [TestCase("", 3)]
        [TestCase("SomeStruct.X>8", 2)]
        [TestCase("SomeStruct.X>16", 1)]
        [TestCase("SomeStruct.X>=16", 2)]
        [TestCase("SumXY<5", 0)]
        [TestCase("SumXY=85", 1)]
        public void TestFiltering(string whereClause, int expectedRowCount)
        {
            var spec = EntitySpec.Begin()
                .Add(EntitySpec.Begin("SelfList").Where(whereClause)
                    .Add("SumXY").Formula("SomeStruct.X+SomeStruct.Y")
                    .Add("SomeStruct.X")
                    .Add("SomeStruct.Y"));

            var theTop = new TheTop
            {
                SelfList = new List<TheTop>
                {
                    new TheTop {SomeStruct = new SomeStruct {X = 42, Y = 43}},
                    new TheTop {SomeStruct = new SomeStruct {X = 3, Y = 3}},
                    new TheTop {SomeStruct = new SomeStruct {X = 16, Y = 13}},
                }
            };
            var tm = new InMemoryTableService();
            var export = new Export<TheTop>(spec, tm);
            export.Run(theTop);
            var tables = tm.GetMergedTables();
            var filteredTable = tables.Single(_ => _.Name == "SelfList");
            Assert.AreEqual(expectedRowCount, filteredTable.Rows.Count);
        }

    }

}
