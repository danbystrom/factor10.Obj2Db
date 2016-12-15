using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests
{
    [TestFixture]
    class LinkedFieldInfoTests
    {
        private class testClass
        {
            public double D;
            public int? I;
        }

        [Test]
        public void TestCohersion()
        {
            var tc = new testClass();
            var lfi1 = new LinkedFieldInfo(tc.GetType(), "D");
            var lfi2 = new LinkedFieldInfo(tc.GetType(), "I");

            var result1 = lfi1.CoherseType(42);
            Assert.AreEqual("Double", result1.GetType().Name);

            var result2 = lfi2.CoherseType(41.2);
            Assert.AreEqual("Int32", result2.GetType().Name);

        }

    }

}
