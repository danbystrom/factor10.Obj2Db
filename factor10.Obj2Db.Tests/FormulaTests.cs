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
                .Add("Double")
                .Add("kalle").Formula("3+Double")
                .Add("nisse").Formula("5*6")
                .Add("sture").Formula("kalle+nisse");

            var export = new Export<TheTop>(spec);
            export.Run(new TheTop {Double = 4});
            CollectionAssert.AreEqual(new[] {4.0, 7.0, 30.0, 37.0}, export.TableManager.GetWithAllData().Single().Rows.Single().Columns);
        }

    }

}
