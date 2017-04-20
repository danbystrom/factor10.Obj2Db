using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests.Database
{
    [TestFixture]
    public class SqlDictionaryTests
    {
        private class WithDictionary
        {
            public AnnoyingThing AnnoyingThing;
        }

        private class AnnoyingThing
        {
            public Dictionary<string, object> Dic;
        }

        [Test]
        public void Test()
        {
            var spec = entitySpec.Begin().Add("*");
            var testObj = new WithDictionary
            {
                AnnoyingThing = new AnnoyingThing
                {
                    Dic = new Dictionary<string, object>
                    {
                        {"nisse", "kalle"},
                        {"sture", null},
                        {"ulrik", 7}
                    }
                }
            };
            SqlTestHelpers.WithNewDb("Gurka", conn =>
            {
                var tm = new SqlTableManager(SqlTestHelpers.ConnectionString("Gurka"));
                var dataExtract = new DataExtract<WithDictionary>(spec, tm);
                dataExtract.Run(testObj);
                var result = SqlTestHelpers.SimpleQuery(conn, "SELECT * FROM WithDictionary");
                Assert.AreEqual(1, result.NameAndTypes.Length);
                result = SqlTestHelpers.SimpleQuery(conn, "SELECT * FROM WithDictionary_AnnoyingThingDic");
                Assert.AreEqual(2, result.NameAndTypes.Length);
            });
        }

    }

}
