using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using factor10.Obj2Db.Tests.TestDataStructures;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests
{
    [TestFixture]
    public class DataExtractCompletedTests
    {
        public class X : IDataExtractCompleted
        {
            public bool CompletedCalled;

            public void Completed()
            {
                CompletedCalled = true;
            }
        }

        [Test]
        public void TestThatCompletedMethodIsCalledOnObjectsImplementingIDataExtractCompleted()
        {
            var export = new DataExtract<X>(entitySpec.Begin().Add("*"));
            var x = new X();
            Assert.IsFalse(x.CompletedCalled);
            export.Run(x);
            Assert.IsTrue(x.CompletedCalled);
        }

    }
}
