using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests
{
    [TestFixture]
    public class StarTests2
    {
#pragma warning disable 649

        private readonly entitySpec _starSpec = entitySpec.Begin().Add("*");

        private class WithDictionary
        {
            public AnnoyingThing AnnoyingThing;
        }

        private class AnnoyingThing
        {
            public Dictionary<string, int> Dic;
        }


        [Test]
        public void TestDictinaryOnNormalLevel()
        {
            var entity = new DataExtract<AnnoyingThing>(_starSpec).TopEntity;
            Assert.AreEqual(0, entity.Fields.Count);
            var list = entity.Lists.Single();
            Assert.AreEqual("Dic", list.ExternalName);
            CollectionAssert.AreEqual(new[] {typeof(string), typeof(int)}, list.Fields.Select(_ => _.FieldType));
        }

        [Test]
        public void TestLiftedDictionary()
        {
            var entity = new DataExtract<WithDictionary>(_starSpec).TopEntity;
            Assert.AreEqual(0, entity.Fields.Count);
            var list = entity.Lists.Single();
            Assert.AreEqual("AnnoyingThingDic", list.ExternalName);
            CollectionAssert.AreEqual(new[] { typeof(string), typeof(int) }, list.Fields.Select(_ => _.FieldType));
        }

    }

}