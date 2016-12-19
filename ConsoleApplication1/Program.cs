using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using factor10.Obj2Db;
using factor10.Obj2Db.Tests.TestData;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            var school = new School
            {
                Name = "Old School",
                Classes = new[] {"Klass 1a", "Klass 1b", "Klass 2a", "Klass 2b", "Klass 3a", "Klass 3b"}.Select(
                    _ => new Class {Name = _, Students = new List<Student>()}).ToList()
            };
            var firstNames = new[] {"Ada", "Bertil", "Cecilia", "David", "Elina", "Fredrik", "Gun", "Hans", "Ida", "Jan", "Klara"};
            var lastNames = new[] {"Johansson", "Eriksson", "Karlsson", "Andersson", "Nilsson", "Svensson", "Pettersson"};
            for (var i = 0; i < 100; i++)
                school.Classes[i%school.Classes.Count].Students.Add(new Student
                {
                    FirstName = firstNames[i%firstNames.Length],
                    LastName = lastNames[i%lastNames.Length]
                });
            var spec = EntitySpec.Begin()
                .Add("Name")
                .Add(EntitySpec.Begin("Classes")
                    .Add("Name")
                    .Add(EntitySpec.Begin("Students")
                        .Add("FirstName")
                        .Add("LastName")));


            const int numberOfSchools = 10000;
            var tableFactory = new SqlTableManager(SqlStuff.ConnectionString("SchoolTest"));
            var export = new Export<School>(spec, tableFactory);
            SqlStuff.WithNewDb("SchoolTest", conn =>
            {
                var sw = Stopwatch.StartNew();
                export.Run(Enumerable.Range(0, numberOfSchools).Select(_ => school));
                Console.WriteLine(sw.ElapsedMilliseconds.ToString());
                sw.Restart();
                Console.WriteLine(sw.ElapsedMilliseconds.ToString());
                //Assert.AreEqual(numberOfSchools, SqlStuff.SimpleQuery<int>(conn, "SELECT count(*) FROM school"));
                //Assert.AreEqual(numberOfSchools * 6, SqlStuff.SimpleQuery<int>(conn, "SELECT count(*) FROM classes"));
                //Assert.AreEqual(numberOfSchools * 100, SqlStuff.SimpleQuery<int>(conn, "SELECT count(*) FROM students"));
            });

            Console.ReadLine();

        }

    }

}
