using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using factor10.Obj2Db.Tests.TestData;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace factor10.Obj2Db.Tests
{
    [TestFixture()]
    public class InverseEntityTests
    {
        [Test]
        public void Test()
        {
            var x = new DataExtract<AllPropertyTypes>(entitySpec.Begin().Add("*"));
            var inverse = new entitySpec(x.TopEntity);
        }
    }
}
