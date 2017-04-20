using System;
using System.Diagnostics;
using System.Linq;
using factor10.Obj2Db.Tests.TestData;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests.Database
{
    public class SqlSchoolTests : SchoolBaseTests
    {
        private const int NumberOfSchools = 100;

        private QueryResult _schoolQueryResult;
        private QueryResult _classQueryResult;
        private QueryResult _studentQueryResult;
        private int _studentCount;

        [OneTimeSetUp]
        public void SetUp2()
        {
            if (Environment.MachineName != "DAN_FACTOR10")
                return;
            var tableFactory = new SqlTableManager(SqlTestHelpers.ConnectionString("SchoolTest"));
            var export = new DataExtract<School>(Spec, tableFactory);
            SqlTestHelpers.WithNewDb("SchoolTest", conn =>
            {
                var sw = Stopwatch.StartNew();
                export.Run(Enumerable.Range(0, NumberOfSchools).Select(_ => School));
                Console.WriteLine(sw.ElapsedMilliseconds.ToString());
                _schoolQueryResult = SqlTestHelpers.SimpleQuery(conn, "SELECT * FROM school");
                _classQueryResult = SqlTestHelpers.SimpleQuery(conn, "SELECT * FROM classes");
                _studentQueryResult = SqlTestHelpers.SimpleQuery(conn, "SELECT TOP 1 * FROM students");
                _studentCount = SqlTestHelpers.SimpleQuery<int>(conn, "SELECT count(*) FROM students");
            });
        }

        [Test]
        public void ThenAllSchoolTableColumnNamesAreCorrect()
        {
            if (Environment.MachineName != "DAN_FACTOR10")
                return;
            CollectionAssert.AreEqual(new[] {"_id_", "Name"}, _schoolQueryResult.NameAndTypes.Select(_ => _.Name));
        }

        [Test]
        public void ThenAllSchoolTableColumnTypesAreCorrect()
        {
            if (Environment.MachineName != "DAN_FACTOR10")
                return;
            CollectionAssert.AreEqual(new[] { typeof(Guid), typeof(string) }, _schoolQueryResult.NameAndTypes.Select(_ => _.Type));
        }

        [Test]
        public void ThenTheCorrectNumberOfSchoolsAreInTheTable()
        {
            Assert.AreEqual(NumberOfSchools, _schoolQueryResult.Rows.Count);
        }

        [Test]
        public void ThenAllClassTableColumnNamesAreCorrect()
        {
            if (Environment.MachineName != "DAN_FACTOR10")
                return;
            CollectionAssert.AreEqual(new[] { "_id_", "_fk_", "Name" }, _classQueryResult.NameAndTypes.Select(_ => _.Name));
        }

        [Test]
        public void ThenAllClassTableColumnTypesAreCorrect()
        {
            if (Environment.MachineName != "DAN_FACTOR10")
                return;
            CollectionAssert.AreEqual(new[] { typeof(Guid), typeof(Guid), typeof(string) }, _classQueryResult.NameAndTypes.Select(_ => _.Type));
        }

        [Test]
        public void ThenTheCorrectNumberOfClassesAreInTheTable()
        {
            Assert.AreEqual(NumberOfSchools*6, _classQueryResult.Rows.Count);
        }

        public void ThenAllStudentTableColumnNamesAreCorrect()
        {
            if (Environment.MachineName != "DAN_FACTOR10")
                return;
            CollectionAssert.AreEqual(new[] { "_fk_", "FirstName", "LastName" }, _studentQueryResult.NameAndTypes.Select(_ => _.Name));
        }

        [Test]
        public void ThenAllStudentTableColumnTypesAreCorrect()
        {
            if (Environment.MachineName != "DAN_FACTOR10")
                return;
            CollectionAssert.AreEqual(new[] { typeof(Guid), typeof(string), typeof(string) }, _studentQueryResult.NameAndTypes.Select(_ => _.Type));
        }

        [Test]
        public void ThenTheCorrectNumberOfStudentsAreInTheTable()
        {
            Assert.AreEqual(NumberOfSchools*100, _studentCount);
        }

    }

}
