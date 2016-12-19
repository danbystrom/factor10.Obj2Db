using System.Collections.Generic;
using System.Linq;
using factor10.Obj2Db.Tests.TestDataStructures;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests
{
    [TestFixture]
    public class EnumTests
    {
        private readonly EntitySpec _spec = EntitySpec.Begin()
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
            var export = new Export<ClassToTestEnumerables>(_spec);
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
            var export = new Export<ClassToTestEnumerables>(_spec, t);
            export.Run(c);
            var tables = export.TableManager.GetWithAllData().ToDictionary(_ => _.Name, _ => _.Rows);
            tables.Remove("ClassToTestEnumerables");
            Assert.IsTrue(tables.Values.All(_ => _.Count == 2));
        }

    }

}
