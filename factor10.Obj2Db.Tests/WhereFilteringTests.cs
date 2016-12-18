using System;
using System.Collections.Generic;
using System.Linq;
using factor10.Obj2Db.Tests.TestData;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests
{

    [TestFixture]
    public class WhereFilteringTests
    {
        [TestCase(null, 3)]
        [TestCase("", 3)]
        [TestCase("SomeStruct.X>8", 2)]
        [TestCase("SomeStruct.X>16", 1)]
        [TestCase("SomeStruct.X>=16", 2)]
        [TestCase("SumXY<5", 0)]
        [TestCase("SumXY==85", 1)]
        [TestCase("SomeStruct.X<SomeStruct.Y", 1)]
        [TestCase("SomeStruct.X==SomeStruct.Y", 1)]
        public void TestFiltering(string whereClause, int expectedRowCount)
        {
            var spec = EntitySpec.Begin()
                .Add(EntitySpec.Begin("SelfList").Where(whereClause)
                    .Add("SumXY").Formula("SomeStruct.X+SomeStruct.Y")
                    .Add("SomeStruct.X")
                    .Add("SomeStruct.Y"));

            var theTop = new TheTop
            {
                SelfList = new List<TheTop>
                {
                    new TheTop {SomeStruct = new SomeStruct {X = 42, Y = 43}},
                    new TheTop {SomeStruct = new SomeStruct {X = 3, Y = 3}},
                    new TheTop {SomeStruct = new SomeStruct {X = 16, Y = 13}},
                }
            };
            var tm = new InMemoryTableManager();
            var export = new Export<TheTop>(spec, tm);
            export.Run(theTop);
            var tables = tm.GetMergedTables();
            var filteredTable = tables.Single(_ => _.Name == "SelfList");
            Assert.AreEqual(expectedRowCount, filteredTable.Rows.Count);
        }

        [Test]
        public void TestThatFilterOnFieldThrows()
        {
            var ex = Assert.Throws<Exception>(() =>
                EntitySpec.Begin()
                    .Add(EntitySpec.Begin("SelfList")
                        .Add("SumXY").Where("SumXY")
                        .Add("SomeStruct.X")
                        .Add("SomeStruct.Y")));
        }

        [Test]
        public void TestFiltering2()
        {
            var aStudent = new Student {FirstName = "Karl", LastName = "Anka"};
            var school = new School
            {
                Name = "Xxx",
                Classes = new List<Class>
                {
                    new Class
                    {
                        Name = "Klass 1",
                        Students = new List<Student> {aStudent,aStudent }
                    },
                    new Class
                    {
                        Name = "Klass 2",
                        Students = new List<Student> {aStudent,aStudent }
                    },
                    new Class
                    {
                        Name = "Klass 3",
                        Students = new List<Student> {aStudent,aStudent }
                    }
                }
            };
            var spec = EntitySpec.Begin()
                .Add(EntitySpec.Begin("Classes").Where("Name!='Klass 1'")
                    .Add("Name")
                    .Add(EntitySpec.Begin("Students")
                        .Add("FirstName")
                        .Add("LastName")));

            var tm = new InMemoryTableManager();
            var export = new Export<School>(spec, tm);
            export.Run(school);
            var tables = tm.GetMergedTables();
            var classesTable = tables.Single(_ => _.Name == "Classes");
            var studentsTable = tables.Single(_ => _.Name == "Students");
            Assert.AreEqual(2, classesTable.Rows.Count);
            // this does not work yet...
            //Assert.AreEqual(4, studentsTable.Rows.Count);
        }

    }

}

