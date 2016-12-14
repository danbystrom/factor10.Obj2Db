using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using factor10.Obj2Db.Tests.TestData;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests
{
    [TestFixture]
    public class SchoolExportTest
    {
        private School _school;
        private EntitySpec _spec;

        [OneTimeSetUp]
        public void SetUp()
        {
            _school = new School
            {
                Name = "Old School",
                Classes = new[] {"Klass 1a", "Klass 1b", "Klass 2a", "Klass 2b", "Klass 3a", "Klass 3b"}.Select(
                    _ => new Class {Name = _, Students = new List<Student>()}).ToList()
            };
            var firstNames = new[] {"Ada", "Bertil", "Cecilia", "David", "Elina", "Fredrik", "Gun", "Hans", "Ida", "Jan", "Klara"};
            var lastNames = new[] {"Johansson", "Eriksson", "Karlsson", "Andersson", "Nilsson", "Svensson", "Pettersson"};
            for (var i = 0; i < 100; i++)
                _school.Classes[i%_school.Classes.Count].Students.Add(new Student
                {
                    FirstName = firstNames[i%firstNames.Length],
                    LastName = lastNames[i%lastNames.Length]
                });

            _spec = EntitySpec.Begin()
                .Add("Name")
                .Add(EntitySpec.Begin("Classes")
                    .Add("Name")
                    .Add(EntitySpec.Begin("Students")
                        .Add("FirstName")
                        .Add("LastName")));
        }

        [Test]
        public void TestSchoolExample()
        {
            var export = new Export<School>(_spec, new InMemoryTableService());
            var tables = export.Run(_school);
            Assert.AreEqual(1, tables.Single(_ => _.Name == "School").Rows.Count);
            Assert.AreEqual(6, tables.Single(_ => _.Name == "Classes").Rows.Count);
            Assert.AreEqual(100, tables.Single(_ => _.Name == "Students").Rows.Count);
        }

        private static List<ITable> mergeTablesHelper(IEnumerable<ITable> allTables)
        {
            var joined = allTables.ToLookup(_ => _.Name, _ => _);
            var result = new List<ITable>();
            foreach (var z in joined)
            {
                var tables = z.ToList();
                for (var i = 1; i < tables.Count; i++)
                    foreach (var tr in tables[i].Rows)
                        tables[0].Rows.Add(tr);
                result.Add(tables[0]);
            }
            return result;
        }

        [Test]
        public void Test100Schools()
        {
            var export = new Export<School>(_spec, new InMemoryTableService());
            var sw = Stopwatch.StartNew();
            var tables = export.Run(Enumerable.Range(0, 100).Select(_ => _school));
            Console.Write(sw.ElapsedMilliseconds.ToString());
            tables = mergeTablesHelper(tables);
            Assert.AreEqual(100, tables.Single(_ => _.Name == "School").Rows.Count);
            Assert.AreEqual(600, tables.Single(_ => _.Name == "Classes").Rows.Count);
            Assert.AreEqual(10000, tables.Single(_ => _.Name == "Students").Rows.Count);
        }

        [Test]
        public void Save100SchoolsToDb()
        {
            const int numberOfSchools = 100;
            var tableFactory = new SqlTableService(SqlStuff.ConnectionString("SchoolTest"));
            var export = new Export<School>(_spec, tableFactory);
            SqlStuff.WithNewDb("SchoolTest", conn =>
            {
                var sw = Stopwatch.StartNew();
                var tables = export.Run(Enumerable.Range(0, numberOfSchools).Select(_ => _school));
                Console.WriteLine(sw.ElapsedMilliseconds.ToString());
                sw.Restart();
                tableFactory.Flush();
                Console.WriteLine(sw.ElapsedMilliseconds.ToString());
                Assert.AreEqual(numberOfSchools, SqlStuff.SimpleQuery<int>(conn, "SELECT count(*) FROM school"));
                Assert.AreEqual(numberOfSchools*6, SqlStuff.SimpleQuery<int>(conn, "SELECT count(*) FROM classes"));
                Assert.AreEqual(numberOfSchools*100, SqlStuff.SimpleQuery<int>(conn, "SELECT count(*) FROM students"));
            });
        }

        [Test, Explicit]
        public void Test10000Schools()
        {
            var export = new Export<School>(_spec, new InMemoryTableService());
            var sw = Stopwatch.StartNew();
            var tables = export.Run(Enumerable.Range(0, 10000).Select(_ => _school));
            Console.Write(sw.ElapsedMilliseconds.ToString());
            tables = mergeTablesHelper(tables);
            Assert.AreEqual(10000, tables.Single(_ => _.Name == "School").Rows.Count);
            Assert.AreEqual(60000, tables.Single(_ => _.Name == "Classes").Rows.Count);
            Assert.AreEqual(1000000, tables.Single(_ => _.Name == "Students").Rows.Count);
        }

    }

}