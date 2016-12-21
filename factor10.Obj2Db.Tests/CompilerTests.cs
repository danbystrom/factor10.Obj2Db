using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using factor10.Obj2Db.Formula;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests
{
    [TestFixture]
    public class CompilerTests
    {
        [Test]
        public void Test()
        {
            var rpn = new Rpn("nisse+2*sture+5");
            var x = new CompileRpn(rpn,
                new List<NameAndType>
                {
                    new NameAndType("nisse", typeof(double)),
                    new NameAndType("sture", typeof(double))
                });
            var result = x.Evaluate(new object[] {1.0, 3.0});
            Console.WriteLine(result);
        }

        [Test]
        public void Test2()
        {
            Console.WriteLine(new CompileRpn(new Rpn("12+13")).Evaluate(null));
        }

    }
}
