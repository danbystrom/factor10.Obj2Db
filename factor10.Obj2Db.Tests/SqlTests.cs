using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using factor10.Obj2Db.Tests.TestData;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests
{
    [TestFixture]
    public class SqlTests
    {
        private string _createSql;
        private AllPropertyTypes _data;
        private ITable _table;

        [OneTimeSetUp]
        public void SetUp()
        {
            _data = new AllPropertyTypes
            {
                TheString = "Ashem Vohu",
                TheDateTime = new DateTime(1967, 10, 1, 1, 2, 3),
                TheDouble = Math.PI,
                TheFloat = 1.835f,
                TheInt16 = 42,
                TheInt32 = 1000000,
                TheInt64 = (long) 1E18,
                TheBool = true,
            };
            var export = new Export<AllPropertyTypes>(EntitySpec.Begin()
                .Add("TheBool")
                .Add("TheString")
                .Add("TheDateTime")
                .Add("TheDouble")
                .Add("TheFloat")
                .Add("TheInt16")
                .Add("TheInt32")
                .Add("TheInt64")
                .Add("TheId")
                .Add("TheEnum")
                .Add("TheNullableInt")
                .Add("TheStringProperty")
                .Add("TheInt32Property")
                .Add("TheNullableInt32Property")
                , new InMemoryTableService());
            _table = export.Run(_data).Single();

            _createSql = SqlStuff.GenerateCreateTable(_table, "");
        }

        [Test]
        public void TestThatCreateStringIsCorrect()
        {
            Assert.AreEqual(
                "[TheInt32] integer not null,[TheInt64] bigint not null,[TheId] uniqueidentifier not null,[TheEnum] integer not null,[TheNullableInt] integer,[TheStringProperty] nvarchar(max),[TheInt32Property] integer not null,[TheNullableInt32Property] integer)",
                _createSql);
        }

        [Test]
        public void TestThatATableCanBeGeneratedFromTheCreateString()
        {
            var didReachItAllTheWay = false;
            SqlStuff.WithNewDb("SqlTests", conn =>
            {
                using (var cmd = new SqlCommand(_createSql, conn))
                    cmd.ExecuteNonQuery();
                using (var cmd = new SqlCommand("SELECT * FROM AllPropertyTypes", conn))
                using (cmd.ExecuteReader())
                    didReachItAllTheWay = true;
            });
            Assert.IsTrue(didReachItAllTheWay);
        }

    }

}