using System.Collections.Generic;
using System.Linq;
using factor10.Obj2Db.Tests.TestData;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests
{
    [TestFixture]
    public class SchoolBaseTests
    {
        protected School School;
        protected entitySpec Spec;

        [OneTimeSetUp]
        public void SetUp()
        {
            School = new School
            {
                Name = "Old School",
                Classes = new[] {"Klass 1a", "Klass 1b", "Klass 2a", "Klass 2b", "Klass 3a", "Klass 3b"}.Select(
                    _ => new Class {Name = _, Students = new List<Student>()}).ToList()
            };
            var firstNames = new[] {"Ada", "Bertil", "Cecilia", "David", "Elina", "Fredrik", "Gun", "Hans", "Ida", "Jan", "Klara"};
            var lastNames = new[] {"Johansson", "Eriksson", "Karlsson", "Andersson", "Nilsson", "Svensson", "Pettersson"};
            for (var i = 0; i < 100; i++)
                School.Classes[i%School.Classes.Count].Students.Add(new Student
                {
                    FirstName = firstNames[i%firstNames.Length],
                    LastName = lastNames[i%lastNames.Length]
                });

            Spec = entitySpec.Begin()
                .Add("Name")
                .Add(entitySpec.Begin("Classes")
                    .Add("Name")
                    .Add(entitySpec.Begin("Students")
                        .Add("FirstName")
                        .Add("LastName")));
        }

    }

}