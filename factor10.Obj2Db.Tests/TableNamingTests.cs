using System.Linq;
using factor10.Obj2Db.Tests.TestData;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests
{
    public class TableNamingBase
    {
        public EntityClass School, Classes, Students;

        public TableNamingBase(entitySpec spec)
        {
            School = new DataExtract<School>(spec).TopEntity;
            Classes = School.Lists.Single();
            Students = Classes.Lists.Single();
        }

    }

    [TestFixture]
    public class TableDefaultNamingTests : TableNamingBase
    {
        public TableDefaultNamingTests()
            : base(
                entitySpec.Begin()
                    .Add("Name")
                    .Add(entitySpec.Begin("Classes")
                        .Add("Name")
                        .Add(entitySpec.Begin("Students")
                            .Add("FirstName")
                            .Add("LastName"))))
        {
        }

        [Test]
        public void TestThatSchoolTableNameIsCorrect()
        {
            Assert.AreEqual("School", School.TableName);
        }

        [Test]
        public void TestThatClassesTableNameIsCorrect()
        {
            Assert.AreEqual("School_Classes", Classes.TableName);
        }

        [Test]
        public void TestThatStudentsTableNameIsCorrect()
        {
            Assert.AreEqual("School_Classes_Students", Students.TableName);
        }

        [Test]
        public void TestThatSchoolForeignKeyNameIsCorrect()
        {
            Assert.AreEqual(null, School.ForeignKeyName);
        }

        [Test]
        public void TestThatClassesForeignKeyNameIsCorrect()
        {
            Assert.AreEqual("School_id_", Classes.ForeignKeyName);
        }

        [Test]
        public void TestThatStudentsForeignKeyNameIsCorrect()
        {
            Assert.AreEqual("School_Classes_id_", Students.ForeignKeyName);
        }

        [Test]
        public void TestThatSchoolPrimaryKeyNameIsCorrect()
        {
            Assert.AreEqual("id_", School.PrimaryKeyName);
        }

        [Test]
        public void TestThatClassesPrimaryKeyNameIsCorrect()
        {
            Assert.AreEqual("id_", Classes.PrimaryKeyName);
        }

        [Test]
        public void TestThatStudentsPrimaryKeyNameIsCorrect()
        {
            Assert.AreEqual("id_", Students.PrimaryKeyName);
        }

    }


    [TestFixture]
    public class TableExternalNamingTests : TableNamingBase
    {
        public TableExternalNamingTests()
            : base(
                entitySpec.Begin(null, "Skola")
                    .Add("Name")
                    .Add(entitySpec.Begin("Classes")
                        .Add("Name")
                        .Add(entitySpec.Begin("Students", "Studenter")
                            .Add("FirstName")
                            .Add("LastName"))))
        {
        }

        [Test]
        public void TestThatSchoolTableNameIsCorrect()
        {
            Assert.AreEqual("Skola", School.TableName);
        }

        [Test]
        public void TestThatClassesTableNameIsCorrect()
        {
            Assert.AreEqual("Skola_Classes", Classes.TableName);
        }

        [Test]
        public void TestThatStudentsTableNameIsCorrect()
        {
            Assert.AreEqual("Studenter", Students.TableName);
        }

        [Test]
        public void TestThatSchoolForeignKeyNameIsCorrect()
        {
            Assert.AreEqual(null, School.ForeignKeyName);
        }

        [Test]
        public void TestThatClassesForeignKeyNameIsCorrect()
        {
            Assert.AreEqual("Skola_id_", Classes.ForeignKeyName);
        }

        [Test]
        public void TestThatStudentsForeignKeyNameIsCorrect()
        {
            Assert.AreEqual("Skola_Classes_id_", Students.ForeignKeyName);
        }

        [Test]
        public void TestThatSchoolPrimaryKeyNameIsCorrect()
        {
            Assert.AreEqual("id_", School.PrimaryKeyName);
        }

        [Test]
        public void TestThatClassesPrimaryKeyNameIsCorrect()
        {
            Assert.AreEqual("id_", Classes.PrimaryKeyName);
        }

        [Test]
        public void TestThatStudentsPrimaryKeyNameIsCorrect()
        {
            Assert.AreEqual("id_", Students.PrimaryKeyName);
        }

    }


    [TestFixture]
    public class TablePrimaryKeyNamingTests : TableNamingBase
    {
        public TablePrimaryKeyNamingTests()
            : base(
                entitySpec.Begin()
                    .Add("Name").PrimaryKey()
                    .Add(entitySpec.Begin("Classes", "Klasser")
                        .Add("Name").PrimaryKey()
                        .Add(entitySpec.Begin("Students")
                            .Add("FirstName")
                            .Add("LastName"))))
        {
        }

        [Test]
        public void TestThatSchoolTableNameIsCorrect()
        {
            Assert.AreEqual("School", School.TableName);
        }

        [Test]
        public void TestThatClassesTableNameIsCorrect()
        {
            Assert.AreEqual("Klasser", Classes.TableName);
        }

        [Test]
        public void TestThatStudentsTableNameIsCorrect()
        {
            Assert.AreEqual("Klasser_Students", Students.TableName);
        }

        [Test]
        public void TestThatSchoolForeignKeyNameIsCorrect()
        {
            Assert.AreEqual(null, School.ForeignKeyName);
        }

        [Test]
        public void TestThatClassesForeignKeyNameIsCorrect()
        {
            Assert.AreEqual("School_Name", Classes.ForeignKeyName);
        }

        [Test]
        public void TestThatStudentsForeignKeyNameIsCorrect()
        {
            Assert.AreEqual("Klasser_Name", Students.ForeignKeyName);
        }

        [Test]
        public void TestThatSchoolPrimaryKeyNameIsCorrect()
        {
            Assert.AreEqual("Name", School.PrimaryKeyName);
        }

        [Test]
        public void TestThatClassesPrimaryKeyNameIsCorrect()
        {
            Assert.AreEqual("Name", Classes.PrimaryKeyName);
        }

        [Test]
        public void TestThatStudentsPrimaryKeyNameIsCorrect()
        {
            Assert.AreEqual("id_", Students.PrimaryKeyName);
        }

    }


    [TestFixture]
    public class TablePrimaryKeyAndFormulaTests : TableNamingBase
    {
        public TablePrimaryKeyAndFormulaTests()
            : base(
                entitySpec.Begin()
                    .Add("Name").PrimaryKey()
                    .Add(entitySpec.Begin("Classes", "Klasser")
                        .Add("Composite").Formula("Name+'_'+#index").PrimaryKey()
                        .Add("Name")
                        .Add(entitySpec.Begin("Students")
                            .Add("FirstName")
                            .Add("LastName"))))
        {
        }

        [Test]
        public void TestThatSchoolTableNameIsCorrect()
        {
            Assert.AreEqual("School", School.TableName);
        }

        [Test]
        public void TestThatClassesTableNameIsCorrect()
        {
            Assert.AreEqual("Klasser", Classes.TableName);
        }

        [Test]
        public void TestThatStudentsTableNameIsCorrect()
        {
            Assert.AreEqual("Klasser_Students", Students.TableName);
        }

        [Test]
        public void TestThatSchoolForeignKeyNameIsCorrect()
        {
            Assert.AreEqual(null, School.ForeignKeyName);
        }

        [Test]
        public void TestThatClassesForeignKeyNameIsCorrect()
        {
            Assert.AreEqual("School_Name", Classes.ForeignKeyName);
        }

        [Test]
        public void TestThatStudentsForeignKeyNameIsCorrect()
        {
            Assert.AreEqual("Klasser_Composite", Students.ForeignKeyName);
        }

        [Test]
        public void TestThatSchoolPrimaryKeyNameIsCorrect()
        {
            Assert.AreEqual("Name", School.PrimaryKeyName);
        }

        [Test]
        public void TestThatClassesPrimaryKeyNameIsCorrect()
        {
            Assert.AreEqual("Composite", Classes.PrimaryKeyName);
        }

        [Test]
        public void TestThatStudentsPrimaryKeyNameIsCorrect()
        {
            Assert.AreEqual("id_", Students.PrimaryKeyName);
        }

    }

}