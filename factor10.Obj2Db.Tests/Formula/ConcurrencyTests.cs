using System;
using System.Collections.Generic;
using System.Linq;
using factor10.Obj2Db.Formula;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests.Formula
{
    [TestFixture]
    public class ConcurrencyTests
    {
        [Test]
        public void Test()
        {
            var rpn = new Rpn("3+(dbl+len(str))/7.0");
            Assert.AreEqual("3 dbl str len( + 7 / +", rpn.ToString());
            var eval = new EvaluateRpn(rpn, new List<NameAndType>
                {new NameAndType("dbl", typeof(double)), new NameAndType("str", typeof(string))});

            var rnd = new Random();
            Enumerable.Range(0, 10000).AsParallel().ForAll(_ =>
            {
                var vars = new object[] {rnd.NextDouble() * 100, new string(' ', rnd.Next(10))};
                var expected = 3 + ((double) vars[0] + ((string) vars[1]).Length) / 7.0;
                Assert.AreEqual(expected, eval.Eval(vars).Numeric);
            });
        }

    }
}
