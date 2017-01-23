using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests.Database
{
    [TestFixture]
    public class SqlBlobTests
    {
        private class WithBlobs
        {
            public byte[] Blob1;
            public byte[] Blob2;
            public byte[] Blob3;
        }

        [Test]
        public void Test()
        {
            var spec = entitySpec.Begin()
                .Add("Blob1")
                .Add("Blob2")
                .Add("Blob3");
            var testObj = new WithBlobs
            {
                Blob1 = new byte[] {1, 2, 3, 4, 5},
                Blob3 = new byte[] {6, 7, 8, 9, 0}
            };
            SqlTestHelpers.WithNewDb("Gurka", conn =>
            {
                var tm = new SqlTableManager(SqlTestHelpers.ConnectionString("Gurka"));
                var dataExtract = new DataExtract<WithBlobs>(spec, tm);
                dataExtract.Run(testObj);
                var result = SqlTestHelpers.SimpleQuery(conn, "SELECT * FROM WithBlobs");
                Assert.AreEqual(3, result.NameAndTypes.Length);
            });
        }

    }

}
