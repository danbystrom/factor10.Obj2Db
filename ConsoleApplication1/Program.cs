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
            var export = new Export<School>(EntitySpec.Begin()
                .Add("Name")
                .Add(EntitySpec.Begin("Classes")
                    .Add("Name")
                    .Add(EntitySpec.Begin("Students")
                        .Add("FirstName")
                        .Add("LastName"))));
            for (var i = 0; i < 3; i++)
            {
                var sw = Stopwatch.StartNew();
                export.Run(Enumerable.Range(0, 10000).Select(_ => school));
                Console.WriteLine(sw.ElapsedMilliseconds.ToString());
                GC.Collect();
            }
            Console.ReadLine();
        }

    }

}
