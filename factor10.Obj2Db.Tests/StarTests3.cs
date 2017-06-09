using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests
{
    [TestFixture]
    public class StarTests3
    {
#pragma warning disable 649
#pragma warning disable 169

        private readonly entitySpec _starSpec = entitySpec.Begin().Add("*");

        // ReSharper disable once ClassNeverInstantiated.Local
        private class SomethingWithCircularRef1
        {
            public int X;
            public SomethingWithCircularRef2 Down;
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class SomethingWithCircularRef2
        {
            public int X;
            public SomethingWithCircularRef1 Up;
        }

        [Test]
        public void TestThatCircularReferenceIsDetected()
        {
            var ex = Assert.Throws<Exception>(() => Entity.Create(null, _starSpec, typeof(SomethingWithCircularRef1), null, true));
            Assert.AreEqual("Circular reference detected while processing inclusion of all fields ('*')", ex.Message);
        }

        [Test]
        public void TestThatCircularReferenceCanOptionallyBeAllowed()
        {
            var entity = Entity.Create(null, _starSpec, typeof(SomethingWithCircularRef1), null, false);
            var spec = new entitySpec(entity);
            var json = JsonConvert.SerializeObject(spec, Formatting.None,
                new JsonSerializerSettings {DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore});
            Assert.AreEqual("{\"name\":\"SomethingWithCircularRef1\",\"fields\":[{\"name\":\"X\",\"type\":\"Int32\"},{\"name\":\"Down.X\","+
                "\"externalname\":\"DownX\",\"type\":\"Int32\"},{\"name\":\"Down.Up\",\"externalname\":\"DownUp\",\"fields\":[{\"name\":\"@\"," +
                "\"externalname\":\"value\",\"type\":\"SomethingWithCircularRef1\"}]}]}", json);
        }

    }

}