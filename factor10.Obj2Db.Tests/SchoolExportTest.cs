using System;
using System.Diagnostics;
using System.Linq;
using factor10.Obj2Db.Tests.TestData;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests
{
    [TestFixture]
    public class SchoolExportTest : SchoolBaseTests
    {

        [Test]
        public void TestSchoolExample()
        {
            var export = new DataExtract<School>(Spec);
            export.Run(School);
            var tables = export.TableManager.GetWithAllData();
            Assert.AreEqual(1, tables.Single(_ => _.Name == "School").Rows.Count);
            Assert.AreEqual(6, tables.Single(_ => _.Name == "Classes").Rows.Count);
            Assert.AreEqual(100, tables.Single(_ => _.Name == "Students").Rows.Count);
        }


        [Test]
        public void Test100Schools()
        {
            var export = new DataExtract<School>(Spec);
            var sw = Stopwatch.StartNew();
            export.Run(Enumerable.Range(0, 100).Select(_ => School));
            Console.Write(sw.ElapsedMilliseconds.ToString());
            var tables = export.TableManager.GetWithAllData();
            Assert.AreEqual(100, tables.Single(_ => _.Name == "School").Rows.Count);
            Assert.AreEqual(600, tables.Single(_ => _.Name == "Classes").Rows.Count);
            Assert.AreEqual(10000, tables.Single(_ => _.Name == "Students").Rows.Count);
        }

        [Test, Explicit]
        public void Test10000Schools()
        {
            var export = new DataExtract<School>(Spec);
            var sw = Stopwatch.StartNew();
            export.Run(Enumerable.Range(0, 10000).Select(_ => School));
            Console.Write(sw.ElapsedMilliseconds.ToString());
            var tables = export.TableManager.GetWithAllData();
            Assert.AreEqual(10000, tables.Single(_ => _.Name == "School").Rows.Count);
            Assert.AreEqual(60000, tables.Single(_ => _.Name == "Classes").Rows.Count);
            Assert.AreEqual(1000000, tables.Single(_ => _.Name == "Students").Rows.Count);
        }

    }

}