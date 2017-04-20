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
            if (SqlTestHelpers.ConnectionString(null) == null)
                return;
            var tableFactory = new SqlTableManager(SqlTestHelpers.ConnectionString("SchoolTest"));
            var export = new DataExtract<School>(Spec, tableFactory);
            SqlTestHelpers.WithNewDb("SchoolTest", conn =>
            {
                var sw = Stopwatch.StartNew();
                export.Run(Enumerable.Range(0, NumberOfSchools).Select(_ => School));
                Console.WriteLine(sw.ElapsedMilliseconds.ToString());
                _schoolQueryResult = SqlTestHelpers.SimpleQuery(conn, "SELECT * FROM school");
                _classQueryResult = SqlTestHelpers.SimpleQuery(conn, "SELECT * FROM school_classes");
                _studentQueryResult = SqlTestHelpers.SimpleQuery(conn, "SELECT TOP 1 * FROM school_classes_students");
                _studentCount = SqlTestHelpers.SimpleQuery<int>(conn, "SELECT count(*) FROM school_classes_students");
            });
        }

        [Test]
        public void ThenAllSchoolTableColumnNamesAreCorrect()
        {
            if (SqlTestHelpers.ConnectionString(null) == null)
                return;
            CollectionAssert.AreEqual(new[] {"_id_", "Name"}, _schoolQueryResult.NameAndTypes.Select(_ => _.Name));
        }

        [Test]
        public void ThenAllSchoolTableColumnTypesAreCorrect()
        {
            if (SqlTestHelpers.ConnectionString(null) == null)
                return;
            CollectionAssert.AreEqual(new[] { typeof(Guid), typeof(string) }, _schoolQueryResult.NameAndTypes.Select(_ => _.Type));
        }

        [Test]
        public void ThenTheCorrectNumberOfSchoolsAreInTheTable()
        {
            if (SqlTestHelpers.ConnectionString(null) == null)
                return;
            Assert.AreEqual(NumberOfSchools, _schoolQueryResult.Rows.Count);
        }

        [Test]
        public void ThenAllClassTableColumnNamesAreCorrect()
        {
            if (SqlTestHelpers.ConnectionString(null) == null)
                return;
            CollectionAssert.AreEqual(new[] { "_id_", "School_id_", "Name" }, _classQueryResult.NameAndTypes.Select(_ => _.Name));
        }

        [Test]
        public void ThenAllClassTableColumnTypesAreCorrect()
        {
            if (SqlTestHelpers.ConnectionString(null) == null)
                return;
            CollectionAssert.AreEqual(new[] { typeof(Guid), typeof(Guid), typeof(string) }, _classQueryResult.NameAndTypes.Select(_ => _.Type));
        }

        [Test]
        public void ThenTheCorrectNumberOfClassesAreInTheTable()
        {
            if (SqlTestHelpers.ConnectionString(null) == null)
                return;
            Assert.AreEqual(NumberOfSchools*6, _classQueryResult.Rows.Count);
        }

        [Test]
        public void ThenAllStudentTableColumnNamesAreCorrect()
        {
            if (SqlTestHelpers.ConnectionString(null) == null)
                return;
            CollectionAssert.AreEqual(new[] { "School_Classes_id_", "FirstName", "LastName" }, _studentQueryResult.NameAndTypes.Select(_ => _.Name));
        }

        [Test]
        public void ThenAllStudentTableColumnTypesAreCorrect()
        {
            if (SqlTestHelpers.ConnectionString(null) == null)
                return;
            CollectionAssert.AreEqual(new[] { typeof(Guid), typeof(string), typeof(string) }, _studentQueryResult.NameAndTypes.Select(_ => _.Type));
        }

        [Test]
        public void ThenTheCorrectNumberOfStudentsAreInTheTable()
        {
            if (SqlTestHelpers.ConnectionString(null) == null)
                return;
            Assert.AreEqual(NumberOfSchools*100, _studentCount);
        }

    }

}
