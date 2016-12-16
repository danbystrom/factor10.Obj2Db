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
            var spec = EntitySpec.Begin()
                .Add("kalle").Formula("3+4")
                .Add("nisse").Formula("5*6")
                .Add("sture").Formula("kalle+nisse");

            var t = new InMemoryTableService();
            var export = new Export<TheTop>(spec, t);
            export.Run(new TheTop());
            CollectionAssert.AreEqual(new[] {7.0, 30.0, 37.0}, t.GetMergedTables().Single().Rows.Single().Columns);
        }

    }

}
