using System.Linq;
using factor10.Obj2Db.Tests.TestData;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests
{
    [TestFixture]
    public class AggregationTests
    {

        [Test]
        public void Test()
        {
            var spec = EntitySpec.Begin()
                .Add(EntitySpec.Begin("Structs").NotSaved()
                    .Add("X").Aggregate(""));
            var theTop = new TheTop {Structs = Enumerable.Range(1, 3).Select(_ => new SomeStruct {X = _}).ToList()};
            var t = new InMemoryTableService();
            var export = new Export<TheTop>(spec, t);
            export.Run(theTop);
            var tables = t.GetMergedTables();
        }
    }
}
